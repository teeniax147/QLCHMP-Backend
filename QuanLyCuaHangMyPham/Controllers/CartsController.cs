using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using QuanLyCuaHangMyPham.Data;
using QuanLyCuaHangMyPham.Models;
using Microsoft.Extensions.Caching.Memory;
using QuanLyCuaHangMyPham.Services;
using Microsoft.Data.SqlClient;

namespace QuanLyCuaHangMyPham.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartsController : ControllerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;
        private readonly IMemoryCache _cache;  // Khai báo _cache
        private readonly IConfiguration _configuration; // Khai báo biến cấu hình
        private readonly ILogger<CartsController> _logger;
        public CartsController(QuanLyCuaHangMyPhamContext context, IMemoryCache cache, IConfiguration configuration, ILogger<CartsController> logger)
        {
            _context = context;
            _cache = cache;
            _configuration = configuration;
            _logger = logger;
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
                    .Select(ci => ci.ProductId) // Chọn các ID sản phẩm để đếm loại
                    .Distinct() // Loại bỏ trùng lặp để đếm số loại sản phẩm
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

                // Lấy thông tin khách hàng từ UserId
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (customer == null)
                {
                    return NotFound("Không tìm thấy thông tin khách hàng.");
                }

                // Lấy giỏ hàng của khách hàng
                var cartItems = await _context.CartItems
                    .Include(ci => ci.Product)
                    .Where(ci => ci.Cart.CustomerId == customer.CustomerId)
                    .Select(ci => new
                    {
                        ProductId = ci.ProductId,
                        ProductName = ci.Product.Name,
                        Quantity = ci.Quantity,
                        UnitPrice = ci.Product.Price,
                        TotalPrice = ci.Quantity * ci.Product.Price,
                        ImageUrl = ci.Product.ImageUrl // Nếu có lưu ảnh sản phẩm
                    })
                    .ToListAsync();

                if (!cartItems.Any())
                {
                    return Ok(new
                    {
                        Message = "Giỏ hàng của bạn đang trống.",
                        CartItems = new List<object>(),
                        TotalAmount = 0
                    });
                }

                // Tính tổng tiền của giỏ hàng
                var totalAmount = cartItems.Sum(ci => ci.TotalPrice);

                return Ok(new
                {
                    CartItems = cartItems,
                    TotalAmount = totalAmount
                });
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
                if (_context == null)
                {
                    return StatusCode(500, new { message = "Database context chưa được khởi tạo." });
                }

                // Lấy giỏ hàng từ session
                var cartItemsJson = HttpContext.Session.GetString("CartItems");

                // Debug: Trả về session data nếu có
                Console.WriteLine($"Session Data: {cartItemsJson}");

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

                // Tạo chuỗi ID cho câu truy vấn SQL
                string idList = string.Join(",", productIds);

                if (string.IsNullOrEmpty(idList))
                {
                    return Ok(new
                    {
                        Message = "Giỏ hàng của bạn đang trống. (idList trống)",
                        CartItems = new List<object>(),
                        TotalAmount = 0,
                        Debug = new { productIds, cartItems }
                    });
                }

                var products = await _context.Products
                    .FromSqlRaw($"SELECT * FROM Products WHERE id IN ({idList})")
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Price,
                        p.ImageUrl
                    })
                    .ToListAsync();

                // Then modify the cart details creation
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

                // Tính tổng tiền giỏ hàng
                var totalAmount = cartDetails.Sum(ci => ci.TotalPrice);

                return Ok(new
                {
                    CartItems = cartDetails,
                    TotalAmount = totalAmount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi lấy chi tiết giỏ hàng.",
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }


        [HttpPost("preview")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> PreviewOrder([FromBody] PreviewOrderRequest request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var customer = await _context.Customers
        .Include(c => c.MembershipLevel) // Lấy thông tin hạng thành viên của khách hàng
        .FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null)
            {
                return BadRequest("Không tìm thấy thông tin khách hàng.");
            }

            var cartItems = await _context.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.Cart.CustomerId == customer.CustomerId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                return BadRequest("Giỏ hàng trống.");
            }

            decimal originalTotalAmount = cartItems.Sum(ci => ci.Product.Price * ci.Quantity);
            decimal discountAmount = 0;
            // Áp dụng giảm giá từ hạng thành viên
            if (customer.MembershipLevel != null)
            {
                discountAmount = originalTotalAmount * (customer.MembershipLevel.DiscountRate / 100);
            }
            if (!string.IsNullOrEmpty(request.CouponCode))
            {
                var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == request.CouponCode);
                if (coupon != null && coupon.QuantityAvailable > 0) // Kiểm tra số lượng còn lại của mã giảm giá
                {
                    discountAmount = coupon.DiscountAmount ?? (coupon.DiscountPercentage.HasValue
                        ? originalTotalAmount * (coupon.DiscountPercentage.Value / 100)
                        : 0);

                    if (coupon.MaxDiscountAmount.HasValue && discountAmount > coupon.MaxDiscountAmount.Value)
                    {
                        discountAmount = coupon.MaxDiscountAmount.Value;
                    }
                }
                else
                {
                    return BadRequest("Mã giảm giá không hợp lệ hoặc đã hết số lượng.");
                }
            }


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

            decimal totalAmount = originalTotalAmount - discountAmount + shippingCost;

            // Lưu dữ liệu tạm thời
            var previewData = new
            {
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

            // Lưu dữ liệu vào IMemoryCache
            _cache.Set($"PreviewOrder:{userId}", previewData, TimeSpan.FromMinutes(30));
            
            return Ok(previewData);
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

        // DTO để ánh xạ sản phẩm
        public class ProductDTO
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public decimal Price { get; set; }
            public string ImageUrl { get; set; }
        }
        // Thêm sản phẩm vào giỏ hàng
        [Authorize(Roles = "Customer")]
        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var cart = await GetOrCreateCartForUser(userId);

                // Kiểm tra sản phẩm đã tồn tại trong giỏ hàng chưa
                var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == request.ProductId);
                if (existingItem != null)
                {
                    // Nếu sản phẩm đã tồn tại, cập nhật số lượng
                    existingItem.Quantity += request.Quantity;
                }
                else
                {
                    // Nếu sản phẩm chưa tồn tại, thêm mới sản phẩm vào giỏ hàng
                    cart.CartItems.Add(new CartItem
                    {
                        ProductId = request.ProductId,
                        Quantity = request.Quantity,
                        AddedAt = DateTime.Now
                    });
                }

                cart.LastUpdated = DateTime.Now;
                await _context.SaveChangesAsync(); // Lưu thay đổi

                return Ok("Sản phẩm đã được thêm vào giỏ hàng.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi thêm sản phẩm vào giỏ hàng.", error = ex.Message });
            }
        }

        [HttpPost("add-guest")]
        public async Task<IActionResult> AddToCartForGuest([FromBody] AddToCartRequest request)
        {
            try
            {
                // Lấy giỏ hàng từ session
                var cartItemsJson = HttpContext.Session.GetString("CartItems");
                List<CartItem> cartItems = string.IsNullOrEmpty(cartItemsJson)
                    ? new List<CartItem>()
                    : JsonConvert.DeserializeObject<List<CartItem>>(cartItemsJson);

                // Kiểm tra sản phẩm đã tồn tại chưa
                var existingItem = cartItems.FirstOrDefault(ci => ci.ProductId == request.ProductId);
                if (existingItem != null)
                {
                    existingItem.Quantity += request.Quantity;
                }
                else
                {
                    cartItems.Add(new CartItem
                    {
                        ProductId = request.ProductId,
                        Quantity = request.Quantity,
                        AddedAt = DateTime.Now
                    });
                }

                // Lưu giỏ hàng vào session
                cartItemsJson = JsonConvert.SerializeObject(cartItems);
                HttpContext.Session.SetString("CartItems", cartItemsJson);

                // Đảm bảo session được lưu
                await HttpContext.Session.CommitAsync();

                return Ok(new
                {
                    message = "Sản phẩm đã được thêm vào giỏ hàng.",
                    cart = cartItems,
                    sessionData = cartItemsJson // Thêm để debug
                });
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
                // Lấy UserId từ thông tin đăng nhập
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                // Lấy giỏ hàng của khách hàng
                var cart = await GetOrCreateCartForUser(userId);

                // Kiểm tra sản phẩm đã có trong giỏ hàng chưa
                var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == request.ProductId);
                if (existingItem != null)
                {
                    // Cập nhật số lượng sản phẩm
                    existingItem.Quantity = request.Quantity;
                    cart.LastUpdated = DateTime.Now; // Cập nhật thời gian giỏ hàng

                    // Lưu thay đổi vào cơ sở dữ liệu
                    await _context.SaveChangesAsync();
                    return Ok("Số lượng sản phẩm đã được cập nhật.");
                }
                else
                {
                    return NotFound("Sản phẩm không tồn tại trong giỏ hàng.");
                }
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
                // Lấy giỏ hàng từ session
                var cartItemsJson = HttpContext.Session.GetString("CartItems");
                if (string.IsNullOrEmpty(cartItemsJson))
                {
                    return BadRequest("Giỏ hàng trống.");
                }

                var cartItems = JsonConvert.DeserializeObject<List<CartItem>>(cartItemsJson);

                // Kiểm tra sản phẩm đã tồn tại trong giỏ hàng chưa
                var existingItem = cartItems.FirstOrDefault(ci => ci.ProductId == request.ProductId);
                if (existingItem != null)
                {
                    // Cập nhật số lượng sản phẩm
                    existingItem.Quantity = request.Quantity;

                    // Lưu giỏ hàng vào session
                    HttpContext.Session.SetString("CartItems", JsonConvert.SerializeObject(cartItems));

                    return Ok("Số lượng sản phẩm đã được cập nhật.");
                }
                else
                {
                    return NotFound("Sản phẩm không tồn tại trong giỏ hàng.");
                }
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
                var cart = await GetOrCreateCartForUser(userId);

                // Tìm sản phẩm trong giỏ hàng
                var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == request.ProductId);
                if (existingItem != null)
                {
                    // Giảm số lượng sản phẩm
                    existingItem.Quantity -= request.Quantity;

                    if (existingItem.Quantity <= 0)
                    {
                        // Nếu số lượng <= 0, xóa sản phẩm khỏi giỏ hàng
                        cart.CartItems.Remove(existingItem);
                    }

                    cart.LastUpdated = DateTime.Now;
                    await _context.SaveChangesAsync(); // Lưu thay đổi
                }
                else
                {
                    return NotFound("Sản phẩm không tồn tại trong giỏ hàng.");
                }

                return Ok("Cập nhật giỏ hàng thành công.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật giỏ hàng.", error = ex.Message });
            }
        }

        // Xóa sản phẩm khỏi giỏ hàng
        [Authorize(Roles = "Customer")]
        [HttpDelete("remove-item")]
        public async Task<IActionResult> RemoveItemFromCart([FromBody] RemoveItemRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var cart = await GetOrCreateCartForUser(userId);

                // Tìm sản phẩm trong giỏ hàng
                var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == request.ProductId);
                if (existingItem != null)
                {
                    // Xóa sản phẩm khỏi giỏ hàng
                    cart.CartItems.Remove(existingItem);

                    cart.LastUpdated = DateTime.Now;
                    await _context.SaveChangesAsync(); // Lưu thay đổi
                }
                else
                {
                    return NotFound("Sản phẩm không tồn tại trong giỏ hàng.");
                }

                return Ok("Sản phẩm đã được xóa khỏi giỏ hàng.");
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
                // Lấy giỏ hàng từ session
                var cartItemsJson = HttpContext.Session.GetString("CartItems");
                if (string.IsNullOrEmpty(cartItemsJson))
                {
                    return BadRequest("Giỏ hàng trống.");
                }

                var cartItems = JsonConvert.DeserializeObject<List<CartItem>>(cartItemsJson);

                // Tìm sản phẩm trong giỏ hàng
                var existingItem = cartItems.FirstOrDefault(ci => ci.ProductId == request.ProductId);
                if (existingItem != null)
                {
                    // Giảm số lượng sản phẩm
                    existingItem.Quantity -= request.Quantity;

                    if (existingItem.Quantity <= 0)
                    {
                        // Nếu số lượng <= 0, xóa sản phẩm khỏi giỏ hàng
                        cartItems.Remove(existingItem);
                    }

                    // Lưu giỏ hàng vào session
                    HttpContext.Session.SetString("CartItems", JsonConvert.SerializeObject(cartItems));

                    return Ok("Cập nhật giỏ hàng thành công.");
                }
                else
                {
                    return NotFound("Sản phẩm không tồn tại trong giỏ hàng.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật giỏ hàng.", error = ex.Message });
            }
        }

        //xóa tất cả sản phẩm trong giỏ hàng
        [HttpDelete("clear")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var cart = await GetOrCreateCartForUser(userId);

                cart.CartItems.Clear();
                cart.LastUpdated = DateTime.Now;

                await _context.SaveChangesAsync(); // Lưu thay đổi

                return Ok("Giỏ hàng đã được xóa sạch.");
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
                // Lấy giỏ hàng từ session
                var cartItemsJson = HttpContext.Session.GetString("CartItems");
                if (string.IsNullOrEmpty(cartItemsJson))
                {
                    return BadRequest("Giỏ hàng trống.");
                }

                var cartItems = JsonConvert.DeserializeObject<List<CartItem>>(cartItemsJson);

                // Xóa tất cả sản phẩm trong giỏ hàng
                cartItems.Clear();

                // Lưu giỏ hàng trống vào session
                HttpContext.Session.SetString("CartItems", JsonConvert.SerializeObject(cartItems));

                return Ok("Giỏ hàng đã được xóa sạch.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xóa giỏ hàng.", error = ex.Message });
            }
        }


        private async Task<Cart> GetOrCreateCartForUser(int userId)
        {
            // Lấy thông tin Customer từ UserId
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null)
            {
                throw new Exception("Khách hàng không tồn tại. Vui lòng kiểm tra thông tin người dùng.");
            }

            // Tìm giỏ hàng dựa trên CustomerId
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId);

            // Nếu không tìm thấy, tạo mới giỏ hàng
            if (cart == null)
            {
                cart = new Cart
                {
                    CustomerId = customer.CustomerId,
                    CreatedAt = DateTime.Now,
                    LastUpdated = DateTime.Now,
                    CartItems = new List<CartItem>()
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync(); // Lưu ngay giỏ hàng mới
            }

            return cart;
        }

        private bool CartExists(int id)
        {
            return _context.Carts.Any(e => e.CartId == id);
        }
        // Các lớp Request
        // Các lớp yêu cầu dữ liệu
        public class AddToCartRequest
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }
        public class UpdateCartRequest
        {
            public int ProductId { get; set; }  // ID sản phẩm cần cập nhật
            public int Quantity { get; set; }   // Số lượng sản phẩm mới
        }

        public class RemoveFromCartRequest
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; } // Số lượng muốn giảm
        }
        public class RemoveItemRequest
        {
            public int ProductId { get; set; } // ID sản phẩm cần xóa
        }
        public class PreviewOrderRequest
        {
            public string? CouponCode { get; set; }
            public int? ShippingCompanyId { get; set; }
            public int? PaymentMethodId { get; set; }
            public string? ShippingAddress { get; set; }
            public string? PhoneNumber { get; set;}
            public string? Email { get; set; }
        }
    }
}
