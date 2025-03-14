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


namespace QuanLyCuaHangMyPham.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CartsController : ControllerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;
        private readonly IMemoryCache _cache;  // Khai báo _cache
        private readonly IConfiguration _configuration; // Khai báo biến cấu hình
        public CartsController(QuanLyCuaHangMyPhamContext context, IMemoryCache cache, IConfiguration configuration)
        {
            _context = context;
            _cache = cache;
            _configuration = configuration;
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

                var cartItems = JsonConvert.DeserializeObject<List<CartItem>>(cartItemsJson);

                // Lấy chi tiết của từng sản phẩm trong giỏ hàng
                var cartDetails = cartItems.Select(ci => new
                {
                    ProductId = ci.ProductId,
                    ProductName = ci.Product.Name,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.Product.Price,
                    TotalPrice = ci.Quantity * ci.Product.Price,
                    ImageUrl = ci.Product.ImageUrl // Nếu có lưu ảnh sản phẩm
                }).ToList();

                if (!cartDetails.Any())
                {
                    return Ok(new
                    {
                        Message = "Giỏ hàng của bạn đang trống.",
                        CartItems = new List<object>(),
                        TotalAmount = 0
                    });
                }

                // Tính tổng tiền của giỏ hàng
                var totalAmount = cartDetails.Sum(ci => ci.TotalPrice);

                return Ok(new
                {
                    CartItems = cartDetails,
                    TotalAmount = totalAmount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy chi tiết giỏ hàng.", error = ex.Message });
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
                PhoneNumber = request.PhoneNumber
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
                    return BadRequest("Giỏ hàng trống.");
                }

                var cartItems = JsonConvert.DeserializeObject<List<CartItem>>(cartItemsJson);

                decimal originalTotalAmount = cartItems.Sum(ci => ci.Product.Price * ci.Quantity);
                decimal discountAmount = 0;

                if (!string.IsNullOrEmpty(request.CouponCode))
                {
                    var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == request.CouponCode);
                    if (coupon != null && coupon.QuantityAvailable > 0)
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
                    PhoneNumber = request.PhoneNumber
                };
                // Lưu thông tin preview vào session
                HttpContext.Session.SetString("PreviewOrderData", JsonConvert.SerializeObject(previewData));
                return Ok(previewData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi tạo preview cho khách vãng lai.", Error = ex.Message });
            }
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
                List<CartItem> cartItems;

                if (string.IsNullOrEmpty(cartItemsJson))
                {
                    // Nếu giỏ hàng chưa có trong session, khởi tạo giỏ hàng mới
                    cartItems = new List<CartItem>();
                }
                else
                {
                    // Deserialize giỏ hàng từ session
                    cartItems = JsonConvert.DeserializeObject<List<CartItem>>(cartItemsJson);
                }

                // Kiểm tra sản phẩm đã tồn tại trong giỏ hàng chưa
                var existingItem = cartItems.FirstOrDefault(ci => ci.ProductId == request.ProductId);
                if (existingItem != null)
                {
                    // Nếu sản phẩm đã tồn tại, cập nhật số lượng
                    existingItem.Quantity += request.Quantity;
                }
                else
                {
                    // Nếu sản phẩm chưa tồn tại, thêm mới sản phẩm vào giỏ hàng
                    cartItems.Add(new CartItem
                    {
                        ProductId = request.ProductId,
                        Quantity = request.Quantity,
                        AddedAt = DateTime.Now
                    });
                }

                // Lưu giỏ hàng vào session
                HttpContext.Session.SetString("CartItems", JsonConvert.SerializeObject(cartItems));

                return Ok("Sản phẩm đã được thêm vào giỏ hàng.");
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
        }
    }
}
