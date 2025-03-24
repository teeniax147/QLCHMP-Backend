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

        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetFeedbacksByProduct(int productId)
        {
            try
            {
                // Lấy dữ liệu cơ bản, không sử dụng projection ngay lập tức
                var feedbackList = await _context.ProductFeedbacks
                    .Where(f => f.ProductId == productId)
                    .ToListAsync();

                if (!feedbackList.Any())
                {
                    return Ok(new List<object>());
                }

                // Lấy thông tin khách hàng và email từ bảng thích hợp
                var result = new List<object>();

                foreach (var feedback in feedbackList)
                {
                    // Lấy thông tin khách hàng
                    var customer = await _context.Customers
                        .Include(c => c.User)
                        .FirstOrDefaultAsync(c => c.CustomerId == feedback.CustomerId);

                    // Lấy email từ đơn hàng gần nhất
                    var latestOrder = await _context.Orders
                        .Where(o => o.CustomerId == feedback.CustomerId)
                        .OrderByDescending(o => o.OrderDate)
                        .FirstOrDefaultAsync();

                    // Tạo đối tượng kết quả
                    result.Add(new
                    {
                        FeedbackId = feedback.FeedbackId,
                        Rating = feedback.Rating,
                        ReviewText = feedback.ReviewText,
                        FeedbackDate = feedback.FeedbackDate,
                        CustomerName = customer?.User != null
                            ? $"{customer.User.FirstName ?? ""} {customer.User.LastName ?? ""}".Trim()
                            : "Khách hàng ẩn danh",
                        Email = latestOrder?.Email
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log lỗi
                Console.WriteLine($"Error in GetFeedbacksByProduct: {ex.Message}");

                return StatusCode(500, "Đã xảy ra lỗi khi lấy đánh giá sản phẩm: " + ex.Message);
            }
        }

        // 2. Lấy danh sách đánh giá của một khách hàng cụ thể
        [Authorize(Roles = "Customer")]
        [HttpGet("customer")]
        public async Task<IActionResult> GetCustomerFeedbacks()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
            {
                return Unauthorized("Không tìm thấy khách hàng.");
            }

            var feedbacks = await _context.ProductFeedbacks
                .Where(f => f.CustomerId == customer.CustomerId)
                .Include(f => f.Product)
                .Select(f => new
                {
                    f.FeedbackId,
                    f.Rating,
                    f.ReviewText,
                    f.FeedbackDate,
                    f.IsEdited,
                    ProductName = f.Product.Name,
                    ProductId = f.ProductId,
                    ProductImage = f.Product.ImageUrl
                })
                .ToListAsync();

            return Ok(feedbacks);
        }

        // 3. Lấy danh sách đánh giá của tất cả khách hàng (Admin)
        [Authorize(Roles = "Admin")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllFeedbacks(int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 10;

            var totalItems = await _context.ProductFeedbacks.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var feedbacks = await _context.ProductFeedbacks
                .Include(f => f.Customer)
                    .ThenInclude(c => c.User)
                .Include(f => f.Product)
                .OrderByDescending(f => f.FeedbackDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new
                {
                    f.FeedbackId,
                    f.Rating,
                    f.ReviewText,
                    f.FeedbackDate,
                    f.IsEdited,
                    CustomerName = f.Customer.User.FirstName + " " + f.Customer.User.LastName,
                    CustomerId = f.CustomerId,
                    ProductName = f.Product.Name,
                    ProductId = f.ProductId
                })
                .ToListAsync();

            return Ok(new
            {
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = page,
                PageSize = pageSize,
                Items = feedbacks
            });
        }

        // 4. Thêm đánh giá sản phẩm
        [HttpPost("add")]
        public async Task<IActionResult> AddProductFeedback([FromBody] AddProductFeedbackRequest request)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized("Không tìm thấy thông tin người dùng.");
            }
            var userId = int.Parse(userIdString);

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
            {
                return Unauthorized("Không tìm thấy khách hàng.");
            }

            // Kiểm tra nếu sản phẩm có tồn tại
            var productExists = await _context.Products.AnyAsync(p => p.Id == request.ProductId);
            if (!productExists)
            {
                return BadRequest("Sản phẩm không tồn tại.");
            }

            // Tính toán ngày tối thiểu cho phép đánh giá (thời điểm hiện tại trừ đi 10 ngày)
            int reviewDeadline = 10; // Số ngày cho phép đánh giá sau khi giao hàng
            var minAllowedDate = DateTime.Now.AddDays(-reviewDeadline);

            // Lấy tất cả đơn hàng đã hoàn thành trong phạm vi thời gian
            var eligibleOrders = await _context.OrderDetails
                .Include(od => od.Order)
                .Where(od => od.ProductId == request.ProductId &&
                       od.Order.CustomerId == customer.CustomerId &&
                       od.Order.Status == "Đã Giao" &&
                       od.Order.OrderDate.HasValue &&
                       od.Order.OrderDate.Value >= minAllowedDate)
                .Select(od => od.Order.Id)
                .Distinct()
                .ToListAsync();

            if (!eligibleOrders.Any())
            {
                return BadRequest($"Bạn chỉ có thể đánh giá sản phẩm trong vòng {reviewDeadline} ngày sau khi nhận hàng.");
            }

            // Lấy tất cả đánh giá hiện tại của khách hàng cho sản phẩm này
            var existingFeedbacks = await _context.ProductFeedbacks
                .Where(f => f.CustomerId == customer.CustomerId && f.ProductId == request.ProductId)
                .ToListAsync();

            // Kiểm tra xem còn đơn hàng nào chưa được đánh giá không
            if (existingFeedbacks.Count >= eligibleOrders.Count)
            {
                return BadRequest("Bạn đã đánh giá cho tất cả các lần mua sản phẩm này trong thời gian cho phép.");
            }

            // Tạo phản hồi mới
            var feedback = new ProductFeedback
            {
                CustomerId = customer.CustomerId,
                ProductId = request.ProductId,
                Rating = request.Rating,
                ReviewText = request.ReviewText,
                FeedbackDate = DateTime.Now,
                IsEdited = false
            };

            _context.ProductFeedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Phản hồi đã được thêm thành công.",
                FeedbackId = feedback.FeedbackId
            });
        }

        // 5. Sửa đánh giá sản phẩm (chỉ được sửa 1 lần)
        [Authorize(Roles = "Customer")]
        [HttpPut("edit/{feedbackId}")]
        public async Task<IActionResult> EditProductFeedback(int feedbackId, [FromBody] EditProductFeedbackRequest request)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized("Không tìm thấy thông tin người dùng.");
            }
            var userId = int.Parse(userIdString);

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
            {
                return Unauthorized("Không tìm thấy khách hàng.");
            }

            // Tìm phản hồi theo FeedbackId
            var feedback = await _context.ProductFeedbacks
                .FirstOrDefaultAsync(f => f.FeedbackId == feedbackId && f.CustomerId == customer.CustomerId);

            if (feedback == null)
            {
                return NotFound("Không tìm thấy phản hồi hoặc bạn không có quyền sửa phản hồi này.");
            }

            // Kiểm tra thời gian đánh giá
            var minAllowedDate = DateTime.Now.AddDays(-10);
            if (feedback.FeedbackDate < minAllowedDate)
            {
                return BadRequest("Bạn chỉ có thể sửa đánh giá trong vòng 10 ngày sau khi đánh giá.");
            }

            // Kiểm tra nếu phản hồi đã được chỉnh sửa trước đó
            if (feedback.IsEdited)
            {
                return BadRequest("Bạn chỉ được phép sửa phản hồi một lần.");
            }

            // Cập nhật phản hồi
            feedback.Rating = request.Rating ?? feedback.Rating;
            if (!string.IsNullOrEmpty(request.ReviewText))
            {
                feedback.ReviewText = request.ReviewText;
            }
            feedback.IsEdited = true;

            _context.ProductFeedbacks.Update(feedback);
            await _context.SaveChangesAsync();

            return Ok("Phản hồi đã được cập nhật thành công.");
        }

        // 6. Xóa phản hồi sản phẩm
        [Authorize(Roles = "Customer")]
        [HttpDelete("delete/{feedbackId}")]
        public async Task<IActionResult> DeleteProductFeedback(int feedbackId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized("Không tìm thấy thông tin người dùng.");
            }
            var userId = int.Parse(userIdString);

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
            {
                return Unauthorized("Không tìm thấy khách hàng.");
            }

            // Tìm phản hồi theo FeedbackId
            var feedback = await _context.ProductFeedbacks
                .FirstOrDefaultAsync(f => f.FeedbackId == feedbackId && f.CustomerId == customer.CustomerId);

            if (feedback == null)
            {
                return NotFound("Không tìm thấy phản hồi hoặc bạn không có quyền xóa phản hồi này.");
            }

            // Xóa phản hồi
            _context.ProductFeedbacks.Remove(feedback);
            await _context.SaveChangesAsync();

            // Không cần gọi UpdateProductRatingStatistics nữa vì trigger đã xử lý

            return Ok("Phản hồi đã được xóa thành công.");
        }

        // Request models
        public class AddProductFeedbackRequest
        {
            [Required(ErrorMessage = "Vui lòng cung cấp ID sản phẩm.")]
            public int ProductId { get; set; }

            [Range(1, 5, ErrorMessage = "Đánh giá phải nằm trong khoảng từ 1 đến 5.")]
            public int? Rating { get; set; }

            [MaxLength(1000, ErrorMessage = "Nội dung phản hồi không được vượt quá 1000 ký tự.")]
            public string? ReviewText { get; set; }
        }

        public class EditProductFeedbackRequest
        {
            [Range(1, 5, ErrorMessage = "Đánh giá phải nằm trong khoảng từ 1 đến 5.")]
            public int? Rating { get; set; }

            [MaxLength(1000, ErrorMessage = "Nội dung phản hồi không được vượt quá 1000 ký tự.")]
            public string? ReviewText { get; set; }
        }
    }
}