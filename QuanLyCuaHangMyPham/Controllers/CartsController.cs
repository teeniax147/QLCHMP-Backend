﻿using System;
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
        // Thêm sản phẩm vào giỏ hàng
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
        //xóa tất cả sản phẩm trong giỏ hàng
        [HttpDelete("clear")]
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

        public class ApplyCouponRequest
        {
            public string CouponCode { get; set; }
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
    }
}