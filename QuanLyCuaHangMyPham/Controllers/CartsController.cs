using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyCuaHangMyPham.Data;
using QuanLyCuaHangMyPham.Models;
using Microsoft.Extensions.Caching.Memory;
using QuanLyCuaHangMyPham.Services;
using QuanLyCuaHangMyPham.Commands.Cart;
using QuanLyCuaHangMyPham.Handlers.Cart;
using QuanLyCuaHangMyPham.Mediators;
using QuanLyCuaHangMyPham.Events;
using QuanLyCuaHangMyPham.Repositories;
using QuanLyCuaHangMyPham.Repositories.Cart;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using static QuanLyCuaHangMyPham.Controllers.ProductsController;

namespace QuanLyCuaHangMyPham.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartsController : ControllerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CartsController> _logger;
        private readonly CartCommandHandler _cartCommandHandler;
        private readonly IMediator _mediator;
        private readonly CartRepository _cartRepository;

        public CartsController(
            QuanLyCuaHangMyPhamContext context,
            IMemoryCache cache,
            IConfiguration configuration,
            ILogger<CartsController> logger,
            CartCommandHandler cartCommandHandler,
            IMediator mediator,
            CartRepository cartRepository)
        {
            _context = context;
            _cache = cache;
            _configuration = configuration;
            _logger = logger;
            _cartCommandHandler = cartCommandHandler;
            _mediator = mediator;
            _cartRepository = cartRepository;
        }

        [HttpGet("item-count")]
        public async Task<IActionResult> GetCartItemCount()
        {
            try
            {
                // Lấy UserId từ token
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                // Lấy thông tin khách hàng từ UserId
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (customer == null)
                {
                    return NotFound("Không tìm thấy thông tin khách hàng.");
                }

                // Đếm số loại sản phẩm trong giỏ hàng
                var itemCount = await _context.CartItems
                    .Where(ci => ci.Cart.CustomerId == customer.CustomerId)
                    .Select(ci => ci.ProductId)
                    .Distinct()
                    .CountAsync();

                return Ok(new { ItemCount = itemCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy số lượng loại sản phẩm trong giỏ hàng.", error = ex.Message });
            }
        }

        [Authorize(Roles = "Customer")]
        [HttpGet("details")]
        public async Task<IActionResult> GetCartDetails()
        {
            try
            {
                // Lấy UserId từ token
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                // Sử dụng Command Pattern
                var command = new GetCartDetailsCommand(_cartRepository, userId);
                var result = await _cartCommandHandler.HandleAsync(command, userId, "GetDetails");

                if (result.Success)
                {
                    return Ok(result.Data);
                }

                return NotFound(result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy chi tiết giỏ hàng.", error = ex.Message });
            }
        }

        [HttpGet("details-guest")]
        public async Task<IActionResult> GetCartDetailsForGuest()
        {
            try
            {
                // Kiểm tra nếu context bị null
                if (_cartRepository == null)
                {
                    return StatusCode(500, new { message = "Repository chưa được khởi tạo." });
                }

                // Sử dụng Command Pattern
                var command = new GetGuestCartDetailsCommand(_cartRepository);
                var result = await _cartCommandHandler.HandleAsync(command, 0, "GetGuestDetails");

                if (result.Success)
                {
                    return Ok(result.Data);
                }

                return Ok(new
                {
                    Message = result.Message,
                    CartItems = new List<object>(),
                    TotalAmount = 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chi tiết giỏ hàng khách vãng lai");
                return StatusCode(500, new { message = "Lỗi khi lấy chi tiết giỏ hàng.", error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpPost("preview")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> PreviewOrder([FromBody] PreviewOrderRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                // Sử dụng Command Pattern
                var command = new PreviewOrderCommand(
                    _cartRepository,
                    userId,
                    request.CouponCode,
                    request.ShippingCompanyId,
                    request.PaymentMethodId,
                    request.ShippingAddress,
                    request.PhoneNumber,
                    request.Email);

                var result = await _cartCommandHandler.HandleAsync(command, userId, "PreviewOrder");

                if (result.Success)
                {
                    return Ok(result.Data);
                }

                return BadRequest(result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo preview đơn hàng.", error = ex.Message });
            }
        }

        [HttpPost("preview-guest")]
        public async Task<IActionResult> PreviewOrderForGuest([FromBody] PreviewOrderRequest request)
        {
            try
            {
                // Lấy giỏ hàng từ session
                var cartItemsJson = HttpContext.Session.GetString("CartItems");
                if (string.IsNullOrEmpty(cartItemsJson))
                {
                    return Ok(new
                    {
                        Message = "Giỏ hàng của bạn đang trống.",
                        CartItems = new List<object>(),
                        TotalAmount = 0
                    });
                }

                // Deserialize giỏ hàng từ session
                var cartItems = JsonConvert.DeserializeObject<List<CartItem>>(cartItemsJson);
                if (cartItems == null || !cartItems.Any())
                {
                    return Ok(new
                    {
                        Message = "Giỏ hàng của bạn đang trống.",
                        CartItems = new List<object>(),
                        TotalAmount = 0
                    });
                }

                // Lấy danh sách ID sản phẩm từ session
                var productIds = cartItems.Select(ci => ci.ProductId).Distinct().ToList();
                if (!productIds.Any())
                {
                    return Ok(new
                    {
                        Message = "Giỏ hàng của bạn đang trống.",
                        CartItems = new List<object>(),
                        TotalAmount = 0
                    });
                }

                // Chuẩn bị danh sách sản phẩm
                List<ProductDTO> products = new List<ProductDTO>();

                // Sử dụng SqlConnection để truy vấn thủ công
                using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();

                    // Tạo câu truy vấn động với danh sách ID sản phẩm
                    string productQuery = $@"
                SELECT id AS Id, 
                       name AS Name, 
                       price AS Price, 
                       image_url AS ImageUrl 
                FROM Products 
                WHERE id IN ({string.Join(",", productIds)})";

                    using (var command = new SqlCommand(productQuery, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                products.Add(new ProductDTO
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    Name = reader.GetString(reader.GetOrdinal("Name")),
                                    Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                                    ImageUrl = reader.IsDBNull(reader.GetOrdinal("ImageUrl"))
                                        ? null
                                        : reader.GetString(reader.GetOrdinal("ImageUrl"))
                                });
                            }
                        }
                    }
                }

                // Kiểm tra xem có sản phẩm nào không
                if (!products.Any())
                {
                    return Ok(new
                    {
                        Message = "Không tìm thấy sản phẩm trong giỏ hàng.",
                        CartItems = new List<object>(),
                        TotalAmount = 0
                    });
                }

                // Tạo chi tiết giỏ hàng
                var cartDetails = cartItems
                    .Join(products,
                        ci => ci.ProductId,
                        p => p.Id,
                        (cartItem, product) => new
                        {
                            ProductId = cartItem.ProductId,
                            ProductName = product.Name,
                            Quantity = cartItem.Quantity,
                            UnitPrice = product.Price,
                            TotalPrice = cartItem.Quantity * product.Price,
                            ImageUrl = product.ImageUrl
                        })
                    .ToList();

                // Tính tổng tiền gốc
                decimal originalTotalAmount = cartDetails.Sum(ci => ci.TotalPrice);

                // Xử lý giảm giá
                decimal discountAmount = 0;
                if (!string.IsNullOrEmpty(request.CouponCode))
                {
                    var coupon = await _context.Coupons
                        .FirstOrDefaultAsync(c => c.Code == request.CouponCode);

                    if (coupon != null && coupon.QuantityAvailable > 0)
                    {
                        discountAmount = coupon.DiscountAmount ??
                            (coupon.DiscountPercentage.HasValue
                                ? originalTotalAmount * (coupon.DiscountPercentage.Value / 100m)
                                : 0);

                        if (coupon.MaxDiscountAmount.HasValue)
                        {
                            discountAmount = Math.Min(discountAmount, coupon.MaxDiscountAmount.Value);
                        }
                    }
                }

                // Xử lý phí vận chuyển
                decimal shippingCost = 0;
                if (request.ShippingCompanyId.HasValue)
                {
                    var shippingCompany = await _context.ShippingCompanies
                        .FirstOrDefaultAsync(sc => sc.Id == request.ShippingCompanyId.Value);

                    if (shippingCompany != null)
                    {
                        shippingCost = shippingCompany.ShippingCost ?? 0;
                    }
                }

                // Tính tổng tiền cuối cùng
                decimal totalAmount = originalTotalAmount - discountAmount + shippingCost;

                // Tạo đối tượng preview
                var previewData = new
                {
                    CartItems = cartDetails,
                    OriginalTotalAmount = originalTotalAmount,
                    DiscountAmount = discountAmount,
                    ShippingCost = shippingCost,
                    TotalAmount = totalAmount,
                    CouponCode = request.CouponCode,
                    ShippingCompanyId = request.ShippingCompanyId,
                    PaymentMethodId = request.PaymentMethodId,
                    ShippingAddress = request.ShippingAddress,
                    PhoneNumber = request.PhoneNumber,
                    Email = request.Email
                };

                // Lưu thông tin preview vào session
                HttpContext.Session.SetString("PreviewOrderData", JsonConvert.SerializeObject(previewData));

                return Ok(previewData);
            }
            catch (Exception ex)
            {
                // Ghi log lỗi chi tiết
                _logger.LogError(ex, "Lỗi khi tạo preview cho khách vãng lai");

                return StatusCode(500, new
                {
                    message = "Lỗi khi tạo preview cho khách vãng lai",
                    error = ex.Message
                });
            }
        }
        public class ProductDTO
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public decimal Price { get; set; }
            public string ImageUrl { get; set; }
        }

        [Authorize(Roles = "Customer")]
        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                // Sử dụng Command Pattern
                var command = new AddToCartCommand(
                    _cartRepository,
                    userId,
                    request.ProductId,
                    request.Quantity);

                var result = await _cartCommandHandler.HandleAsync(
                    command,
                    userId,
                    "Add",
                    request.ProductId,
                    request.Quantity);

                if (result.Success)
                {
                    return Ok(result.Message);
                }

                return StatusCode(500, new { message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi thêm sản phẩm vào giỏ hàng.", error = ex.Message });
            }
        }

        // CartsController.cs - Phương thức AddToCartForGuest
        [HttpPost("add-guest")]
        public async Task<IActionResult> AddToCartForGuest([FromBody] AddToCartRequest request)
        {
            try
            {
                // Sử dụng Command Pattern
                var command = new AddToGuestCartCommand(
                    _cartRepository,
                    request.ProductId,
                    request.Quantity);

                var result = await _cartCommandHandler.HandleAsync(
                    command,
                    0,
                    "AddGuest",
                    request.ProductId,
                    request.Quantity);

                if (result.Success)
                {
                    return Ok(new
                    {
                        message = result.Message,
                        cart = result.Data
                    });
                }

                return StatusCode(500, new { message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi thêm sản phẩm vào giỏ hàng.", error = ex.Message });
            }
        }

        [HttpPut("update")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> UpdateCartQuantity([FromBody] UpdateCartRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                // Sử dụng Command Pattern
                var command = new UpdateCartCommand(
                    _cartRepository,
                    userId,
                    request.ProductId,
                    request.Quantity);

                var result = await _cartCommandHandler.HandleAsync(
                    command,
                    userId,
                    "Update",
                    request.ProductId,
                    request.Quantity);

                if (result.Success)
                {
                    return Ok(result.Message);
                }

                return NotFound(result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật giỏ hàng.", error = ex.Message });
            }
        }

        [HttpPut("update-guest")]
        public async Task<IActionResult> UpdateCartQuantityForGuest([FromBody] UpdateCartRequest request)
        {
            try
            {
                // Sử dụng Command Pattern
                var command = new UpdateGuestCartCommand(
                    _cartRepository,
                    request.ProductId,
                    request.Quantity);

                var result = await _cartCommandHandler.HandleAsync(
                    command,
                    0,
                    "UpdateGuest",
                    request.ProductId,
                    request.Quantity);

                if (result.Success)
                {
                    return Ok(result.Message);
                }

                return NotFound(result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật giỏ hàng.", error = ex.Message });
            }
        }

        [Authorize(Roles = "Customer")]
        [HttpPost("remove")]
        public async Task<IActionResult> RemoveFromCart([FromBody] RemoveFromCartRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                // Sử dụng Command Pattern
                var command = new RemoveFromCartCommand(
                    _cartRepository,
                    userId,
                    request.ProductId,
                    request.Quantity);

                var result = await _cartCommandHandler.HandleAsync(
                    command,
                    userId,
                    "Remove",
                    request.ProductId,
                    request.Quantity);

                if (result.Success)
                {
                    return Ok(result.Message);
                }

                return NotFound(result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật giỏ hàng.", error = ex.Message });
            }
        }

        [Authorize(Roles = "Customer")]
        [HttpDelete("remove-item")]
        public async Task<IActionResult> RemoveItemFromCart([FromBody] RemoveItemRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                // Sử dụng Command Pattern
                var command = new RemoveItemCommand(
                    _cartRepository,
                    userId,
                    request.ProductId);

                var result = await _cartCommandHandler.HandleAsync(
                    command,
                    userId,
                    "RemoveItem",
                    request.ProductId);

                if (result.Success)
                {
                    return Ok(result.Message);
                }

                return NotFound(result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xóa sản phẩm khỏi giỏ hàng.", error = ex.Message });
            }
        }

        [HttpPost("remove-guest")]
        public async Task<IActionResult> RemoveFromCartForGuest([FromBody] RemoveFromCartRequest request)
        {
            try
            {
                // Sử dụng Command Pattern
                var command = new RemoveFromGuestCartCommand(
                    _cartRepository,
                    request.ProductId,
                    request.Quantity);

                var result = await _cartCommandHandler.HandleAsync(
                    command,
                    0,
                    "RemoveGuest",
                    request.ProductId,
                    request.Quantity);

                if (result.Success)
                {
                    return Ok(result.Message);
                }

                return NotFound(result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật giỏ hàng.", error = ex.Message });
            }
        }

        [HttpDelete("clear")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                // Sử dụng Command Pattern
                var command = new ClearCartCommand(_cartRepository, userId);
                var result = await _cartCommandHandler.HandleAsync(command, userId, "Clear");

                if (result.Success)
                {
                    return Ok(result.Message);
                }

                return StatusCode(500, new { message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xóa giỏ hàng.", error = ex.Message });
            }
        }

        [HttpDelete("clear-guest")]
        public async Task<IActionResult> ClearCartForGuest()
        {
            try
            {
                // Sử dụng Command Pattern
                var command = new ClearGuestCartCommand(_cartRepository);
                var result = await _cartCommandHandler.HandleAsync(command, 0, "ClearGuest");

                if (result.Success)
                {
                    return Ok(result.Message);
                }

                return BadRequest(result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xóa giỏ hàng.", error = ex.Message });
            }
        }

        // Các lớp Request
        public class AddToCartRequest
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }

        public class UpdateCartRequest
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }

        public class RemoveFromCartRequest
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }

        public class RemoveItemRequest
        {
            public int ProductId { get; set; }
        }

        public class PreviewOrderRequest
        {
            public string? CouponCode { get; set; }
            public int? ShippingCompanyId { get; set; }
            public int? PaymentMethodId { get; set; }
            public string? ShippingAddress { get; set; }
            public string? PhoneNumber { get; set; }
            public string? Email { get; set; }
        }
    }
}