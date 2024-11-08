using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyCuaHangMyPham.Data;
using QuanLyCuaHangMyPham.Models;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace QuanLyCuaHangMyPham.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CouponsController : ControllerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;

        public CouponsController(QuanLyCuaHangMyPhamContext context)
        {
            _context = context;
        }

        // Lấy danh sách mã giảm giá
        [HttpGet]
        public async Task<IActionResult> GetCoupons()
        {
            var coupons = await _context.Coupons.ToListAsync();
            return Ok(coupons);
        }

        // Lấy mã giảm giá theo ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCoupon(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
            {
                return NotFound(new { message = "Mã giảm giá không tồn tại." });
            }
            return Ok(coupon);
        }
        // Thêm mã giảm giá mới (chỉ dành cho Admin)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateCoupon([FromBody] Coupon coupon)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Kiểm tra mã code có tồn tại chưa
            if (await _context.Coupons.AnyAsync(c => c.Code == coupon.Code))
            {
                return Conflict(new { message = "Mã giảm giá này đã tồn tại." });
            }

            coupon.CreatedAt = DateTime.Now;
            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCoupon), new { id = coupon.Id }, coupon);
        }

        // Cập nhật mã giảm giá (chỉ dành cho Admin)
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCoupon(int id, [FromBody] Coupon coupon)
        {
            if (id != coupon.Id)
            {
                return BadRequest(new { message = "ID không hợp lệ." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingCoupon = await _context.Coupons.FindAsync(id);
            if (existingCoupon == null)
            {
                return NotFound(new { message = "Mã giảm giá không tồn tại." });
            }

            // Cập nhật thông tin mã giảm giá
            existingCoupon.Name = coupon.Name;
            existingCoupon.Code = coupon.Code;
            existingCoupon.DiscountAmount = coupon.DiscountAmount;
            existingCoupon.DiscountPercentage = coupon.DiscountPercentage;
            existingCoupon.MaxDiscountAmount = coupon.MaxDiscountAmount;
            existingCoupon.StartDate = coupon.StartDate;
            existingCoupon.EndDate = coupon.EndDate;
            existingCoupon.MinimumOrderAmount = coupon.MinimumOrderAmount;
            existingCoupon.QuantityAvailable = coupon.QuantityAvailable;

            _context.Coupons.Update(existingCoupon);
            await _context.SaveChangesAsync();

            return Ok(existingCoupon);
        }
        // Xóa mã giảm giá (chỉ dành cho Admin)
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCoupon(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
            {
                return NotFound(new { message = "Mã giảm giá không tồn tại." });
            }

            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa mã giảm giá thành công." });
        }

        // Kiểm tra mã giảm giá hợp lệ
        [HttpPost("validate")]
        public async Task<IActionResult> ValidateCoupon([FromBody] ValidateCouponRequest request)
        {
            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == request.Code);
            if (coupon == null)
            {
                return BadRequest(new { message = "Mã giảm giá không hợp lệ." });
            }

            // Kiểm tra ngày có hiệu lực
            var currentDate = DateOnly.FromDateTime(DateTime.Now);
            if (coupon.StartDate.HasValue && currentDate < coupon.StartDate ||
                coupon.EndDate.HasValue && currentDate > coupon.EndDate)
            {
                return BadRequest(new { message = "Mã giảm giá đã hết hạn." });
            }

            // Kiểm tra số lượng mã còn khả dụng
            if (coupon.QuantityAvailable <= 0)
            {
                return BadRequest(new { message = "Mã giảm giá đã hết số lượng." });
            }

            // Kiểm tra giá trị đơn hàng tối thiểu
            if (request.OrderAmount < coupon.MinimumOrderAmount)
            {
                return BadRequest(new { message = $"Đơn hàng phải có giá trị tối thiểu là {coupon.MinimumOrderAmount} để sử dụng mã giảm giá này." });
            }

            // Tính toán mức giảm giá
            decimal discount = 0;
            if (coupon.DiscountAmount.HasValue)
            {
                discount = coupon.DiscountAmount.Value;
            }
            else if (coupon.DiscountPercentage.HasValue)
            {
                discount = request.OrderAmount * (coupon.DiscountPercentage.Value / 100);
                if (coupon.MaxDiscountAmount.HasValue && discount > coupon.MaxDiscountAmount.Value)
                {
                    discount = coupon.MaxDiscountAmount.Value;
                }
            }

            return Ok(new
            {
                message = "Mã giảm giá hợp lệ.",
                discount = discount,
                finalAmount = request.OrderAmount - discount
            });
        }
        // Cập nhật số lượng mã giảm giá thủ công (chỉ dành cho Admin)
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/update-quantity")]
        public async Task<IActionResult> UpdateCouponQuantity(int id, [FromBody] UpdateQuantityRequest request)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
            {
                return NotFound(new { message = "Mã giảm giá không tồn tại." });
            }

            coupon.QuantityAvailable = request.NewQuantity;
            _context.Coupons.Update(coupon);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã cập nhật số lượng mã giảm giá thành công.", Quantity = coupon.QuantityAvailable });
        }

        private bool CouponExists(int id)
        {
            return _context.Coupons.Any(e => e.Id == id);
        }

        public class CreateCouponRequest
        {
            [Required(ErrorMessage = "Tên mã giảm giá là bắt buộc.")]
            [MaxLength(100, ErrorMessage = "Tên mã giảm giá không được vượt quá 100 ký tự.")]
            public string Name { get; set; }

            [Required(ErrorMessage = "Mã code là bắt buộc.")]
            [RegularExpression("^[A-Z0-9]{5,10}$", ErrorMessage = "Mã code chỉ được chứa chữ hoa và số, từ 5 đến 10 ký tự.")]
            public string Code { get; set; }

            [Range(0, double.MaxValue, ErrorMessage = "Số tiền giảm giá phải lớn hơn hoặc bằng 0.")]
            public decimal? DiscountAmount { get; set; }

            [Range(0, 100, ErrorMessage = "Phần trăm giảm giá phải nằm trong khoảng từ 0 đến 100.")]
            public decimal? DiscountPercentage { get; set; }

            [Range(0, double.MaxValue, ErrorMessage = "Số tiền giảm tối đa phải lớn hơn hoặc bằng 0.")]
            public decimal? MaxDiscountAmount { get; set; }

            public DateOnly? StartDate { get; set; }
            public DateOnly? EndDate { get; set; }

            [Range(0, double.MaxValue, ErrorMessage = "Số tiền tối thiểu của đơn hàng phải lớn hơn hoặc bằng 0.")]
            public decimal? MinimumOrderAmount { get; set; }

            [Range(0, int.MaxValue, ErrorMessage = "Số lượng mã phải lớn hơn hoặc bằng 0.")]
            public int? QuantityAvailable { get; set; }
        }

        public class UpdateCouponRequest
        {
            [Required(ErrorMessage = "ID mã giảm giá là bắt buộc.")]
            public int Id { get; set; }

            [Required(ErrorMessage = "Tên mã giảm giá là bắt buộc.")]
            [MaxLength(100, ErrorMessage = "Tên mã giảm giá không được vượt quá 100 ký tự.")]
            public string Name { get; set; }

            [Required(ErrorMessage = "Mã code là bắt buộc.")]
            [RegularExpression("^[A-Z0-9]{5,10}$", ErrorMessage = "Mã code chỉ được chứa chữ hoa và số, từ 5 đến 10 ký tự.")]
            public string Code { get; set; }

            [Range(0, double.MaxValue, ErrorMessage = "Số tiền giảm giá phải lớn hơn hoặc bằng 0.")]
            public decimal? DiscountAmount { get; set; }

            [Range(0, 100, ErrorMessage = "Phần trăm giảm giá phải nằm trong khoảng từ 0 đến 100.")]
            public decimal? DiscountPercentage { get; set; }

            [Range(0, double.MaxValue, ErrorMessage = "Số tiền giảm tối đa phải lớn hơn hoặc bằng 0.")]
            public decimal? MaxDiscountAmount { get; set; }

            public DateOnly? StartDate { get; set; }
            public DateOnly? EndDate { get; set; }

            [Range(0, double.MaxValue, ErrorMessage = "Số tiền tối thiểu của đơn hàng phải lớn hơn hoặc bằng 0.")]
            public decimal? MinimumOrderAmount { get; set; }

            [Range(0, int.MaxValue, ErrorMessage = "Số lượng mã phải lớn hơn hoặc bằng 0.")]
            public int? QuantityAvailable { get; set; }
        }
        // Request model cho việc cập nhật số lượng mã giảm giá
        public class UpdateQuantityRequest
        {
            [Required(ErrorMessage = "Vui lòng nhập số lượng mới.")]
            [Range(0, int.MaxValue, ErrorMessage = "Số lượng phải là số nguyên dương.")]
            public int NewQuantity { get; set; }
        }
        public class ValidateCouponRequest
        {
            [Required(ErrorMessage = "Mã code là bắt buộc.")]
            public string Code { get; set; }

            [Required(ErrorMessage = "Số tiền đơn hàng là bắt buộc.")]
            [Range(0, double.MaxValue, ErrorMessage = "Số tiền đơn hàng phải lớn hơn hoặc bằng 0.")]
            public decimal OrderAmount { get; set; }
        }
    }
}
