using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    public class ProductFeedbacksController : ControllerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;

        public ProductFeedbacksController(QuanLyCuaHangMyPhamContext context)
        {
            _context = context;
        }

        // Lấy danh sách phản hồi của một sản phẩm
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetFeedbacksByProduct(int productId)
        {
            var feedbacks = await _context.ProductFeedbacks
                .Where(f => f.ProductId == productId)
                .Include(f => f.Customer)
                .Select(f => new
                {
                    f.FeedbackId,
                    f.Rating,
                    f.ReviewText,
                    f.FeedbackDate,
                    CustomerName = f.Customer.User.FirstName + " " + f.Customer.User.LastName
                })
                .ToListAsync();

            if (!feedbacks.Any())
            {
                return NotFound("Không có phản hồi nào cho sản phẩm này.");
            }

            return Ok(feedbacks);
        }

        // Thêm phản hồi cho sản phẩm
        [Authorize(Roles = "Customer")]
        [HttpPost("add")]
        public async Task<IActionResult> AddProductFeedback([FromBody] AddProductFeedbackRequest request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
            {
                return Unauthorized("Không tìm thấy khách hàng.");
            }

            // Kiểm tra nếu khách hàng đã mua sản phẩm này
            var hasPurchased = await _context.OrderDetails
                .Include(od => od.Order)
                .AnyAsync(od => od.ProductId == request.ProductId && od.Order.CustomerId == customer.CustomerId);

            if (!hasPurchased)
            {
                return BadRequest("Bạn chỉ có thể đánh giá sản phẩm mà bạn đã mua.");
            }

            // Kiểm tra nếu khách hàng đã đánh giá sản phẩm này cho đơn hàng
            var hasAlreadyReviewed = await _context.ProductFeedbacks
                .AnyAsync(f => f.CustomerId == customer.CustomerId && f.ProductId == request.ProductId);

            if (hasAlreadyReviewed)
            {
                return BadRequest("Bạn đã đánh giá sản phẩm này cho đơn hàng này.");
            }

            var feedback = new ProductFeedback
            {
                CustomerId = customer.CustomerId,
                ProductId = request.ProductId,
                Rating = request.Rating,
                ReviewText = request.ReviewText,
                FeedbackDate = DateTime.Now
            };

            _context.ProductFeedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            return Ok("Phản hồi đã được thêm thành công.");
        }

        // Xóa phản hồi sản phẩm (chỉ dành cho chủ sở hữu phản hồi)
        [Authorize(Roles = "Customer")]
        [HttpDelete("delete/{feedbackId}")]
        public async Task<IActionResult> DeleteProductFeedback(int feedbackId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Tìm phản hồi theo FeedbackId
            var feedback = await _context.ProductFeedbacks
                .Include(f => f.Customer)
                .FirstOrDefaultAsync(f => f.FeedbackId == feedbackId);

            if (feedback == null)
            {
                return NotFound("Không tìm thấy phản hồi.");
            }

            // Kiểm tra nếu khách hàng hiện tại là chủ sở hữu của phản hồi
            if (feedback.Customer.UserId != userId)
            {
                return Forbid("Bạn không có quyền xóa phản hồi này.");
            }

            // Xóa phản hồi
            _context.ProductFeedbacks.Remove(feedback);
            await _context.SaveChangesAsync();

            return Ok("Phản hồi đã được xóa thành công.");
        }


        private bool ProductFeedbackExists(int id)
        {
            return _context.ProductFeedbacks.Any(e => e.FeedbackId == id);
        }
        public class AddProductFeedbackRequest
        {
            [Required(ErrorMessage = "Vui lòng cung cấp ID sản phẩm.")]
            public int ProductId { get; set; }

            [Range(1, 5, ErrorMessage = "Đánh giá phải nằm trong khoảng từ 1 đến 5.")]
            public int? Rating { get; set; }

            [MaxLength(1000, ErrorMessage = "Nội dung phản hồi không được vượt quá 1000 ký tự.")]
            public string? ReviewText { get; set; }
        }
    }
}
