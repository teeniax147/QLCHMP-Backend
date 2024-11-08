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

namespace QuanLyCuaHangMyPham.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CartsController : ControllerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;

        public CartsController(QuanLyCuaHangMyPhamContext context)
        {
            _context = context;
        }

        // Lấy chi tiết giỏ hàng
        [HttpGet("checkout")]
        public async Task<IActionResult> Checkout()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.CustomerId == userId);

            if (cart == null || !cart.CartItems.Any())
            {
                return BadRequest("Giỏ hàng trống.");
            }

            var totalAmount = cart.CartItems.Sum(ci => ci.Product.Price * ci.Quantity);

            return Ok(new
            {
                CartItems = cart.CartItems.Select(ci => new
                {
                    ci.ProductId,
                    ci.Product.Name,
                    ci.Product.Price,
                    ci.Quantity,
                    TotalPrice = ci.Product.Price * ci.Quantity
                }),
                TotalAmount = totalAmount
            });
        }
        // Thêm sản phẩm vào giỏ hàng
        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var cart = await GetOrCreateCartForUser(userId);

            var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == request.ProductId);
            if (existingItem != null)
            {
                existingItem.Quantity += request.Quantity;
            }
            else
            {
                cart.CartItems.Add(new CartItem
                {
                    ProductId = request.ProductId,
                    Quantity = request.Quantity,
                    AddedAt = DateTime.Now
                });
            }

            cart.LastUpdated = DateTime.Now;
            await _context.SaveChangesAsync();
            return Ok("Sản phẩm đã được thêm vào giỏ hàng.");
        }
        // Cập nhật số lượng sản phẩm trong giỏ hàng
        [HttpPut("update")]
        public async Task<IActionResult> UpdateCartItem([FromBody] UpdateCartItemRequest request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.CustomerId == userId);

            if (cart == null)
            {
                return NotFound("Không tìm thấy giỏ hàng.");
            }

            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == request.ProductId);
            if (cartItem == null)
            {
                return NotFound("Không tìm thấy sản phẩm trong giỏ hàng.");
            }

            cartItem.Quantity = request.Quantity;
            cart.LastUpdated = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok("Đã cập nhật số lượng sản phẩm.");
        }

        // Xóa sản phẩm khỏi giỏ hàng
        [HttpDelete("remove/{productId}")]
        public async Task<IActionResult> RemoveCartItem(int productId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.CustomerId == userId);

            if (cart == null)
            {
                return NotFound("Không tìm thấy giỏ hàng.");
            }

            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
            if (cartItem == null)
            {
                return NotFound("Không tìm thấy sản phẩm trong giỏ hàng.");
            }

            cart.CartItems.Remove(cartItem);
            cart.LastUpdated = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok("Đã xóa sản phẩm khỏi giỏ hàng.");
        }

        // Áp dụng mã giảm giá
        // Áp dụng mã giảm giá tạm thời cho giỏ hàng
        [HttpPost("apply-coupon")]
        public async Task<IActionResult> ApplyCouponToCart([FromBody] ApplyCouponRequest request)
        {
            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == request.CouponCode);
            if (coupon == null || coupon.QuantityAvailable <= 0 ||
    (coupon.StartDate.HasValue && coupon.StartDate > DateOnly.FromDateTime(DateTime.Now)) ||
    (coupon.EndDate.HasValue && coupon.EndDate < DateOnly.FromDateTime(DateTime.Now)))
            {
                return BadRequest("Mã giảm giá không hợp lệ hoặc đã hết hạn.");
            }

            var discountAmount = coupon.DiscountAmount ?? 0;
            if (coupon.DiscountPercentage.HasValue)
            {
                discountAmount += (coupon.DiscountPercentage.Value / 100);
            }

            return Ok(new { DiscountAmount = discountAmount });
        }
        private async Task<Cart> GetOrCreateCartForUser(int userId)
        {
            var cart = await _context.Carts.Include(c => c.CartItems).FirstOrDefaultAsync(c => c.CustomerId == userId);
            if (cart == null)
            {
                cart = new Cart
                {
                    CustomerId = userId,
                    CreatedAt = DateTime.Now,
                    LastUpdated = DateTime.Now,
                    CartItems = new List<CartItem>()
                };
                _context.Carts.Add(cart);
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

        public class UpdateCartItemRequest
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }

        public class ApplyCouponRequest
        {
            public string CouponCode { get; set; }
        }
    }
}
