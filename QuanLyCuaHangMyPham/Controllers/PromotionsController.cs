using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuanLyCuaHangMyPham.Data;
using QuanLyCuaHangMyPham.Models;
using QuanLyCuaHangMyPham.Services.PROMOTIONS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyCuaHangMyPham.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PromotionController : ControllerBase
    {
        private readonly IPromotionService _promotionService;
        private readonly ILogger<PromotionController> _logger;

        public PromotionController(
            IPromotionService promotionService,
            ILogger<PromotionController> logger)
        {
            _promotionService = promotionService;
            _logger = logger;
        }

        // GET: api/Promotion
        [HttpGet]
        public async Task<IActionResult> GetPromotions()
        {
            try
            {
                var promotions = await _promotionService.GetPromotionsAsync();
                return Ok(promotions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving promotions");
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách khuyến mãi." });
            }
        }

        // GET: api/Promotion/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPromotion(int id)
        {
            try
            {
                var promotion = await _promotionService.GetPromotionByIdAsync(id);
                if (promotion == null)
                {
                    return NotFound(new { message = "Không tìm thấy khuyến mãi." });
                }
                return Ok(promotion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving promotion with ID: {id}");
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin khuyến mãi." });
            }
        }

        // POST: api/Promotion
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreatePromotion([FromBody] CreatePromotionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var promotion = await _promotionService.CreatePromotionAsync(request);
                return CreatedAtAction(nameof(GetPromotion), new { id = promotion.Id }, promotion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating promotion");
                return StatusCode(500, new { message = "Lỗi khi tạo khuyến mãi." });
            }
        }

        // PUT: api/Promotion/{id}
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePromotion(int id, [FromBody] UpdatePromotionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var promotion = await _promotionService.UpdatePromotionAsync(id, request);
                if (promotion == null)
                {
                    return NotFound(new { message = "Không tìm thấy khuyến mãi." });
                }
                return Ok(promotion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating promotion with ID: {id}");
                return StatusCode(500, new { message = "Lỗi khi cập nhật khuyến mãi." });
            }
        }

        // DELETE: api/Promotion/{id}
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePromotion(int id)
        {
            try
            {
                var result = await _promotionService.DeletePromotionAsync(id);
                if (!result)
                {
                    return NotFound(new { message = "Không tìm thấy khuyến mãi." });
                }
                return Ok(new { message = "Khuyến mãi đã được xóa thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting promotion with ID: {id}");
                return StatusCode(500, new { message = "Lỗi khi xóa khuyến mãi." });
            }
        }

        // GET: api/Promotion/active
        [HttpGet("active")]
        public async Task<IActionResult> GetActivePromotions()
        {
            try
            {
                var activePromotions = await _promotionService.GetActivePromotionsAsync();
                return Ok(activePromotions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active promotions");
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách khuyến mãi đang hoạt động." });
            }
        }

        // GET: api/Promotion/product/{productId}
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetPromotionsByProduct(int productId)
        {
            try
            {
                var promotions = await _promotionService.GetPromotionsByProductAsync(productId);
                return Ok(promotions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving promotions for product ID: {productId}");
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách khuyến mãi theo sản phẩm." });
            }
        }

        // GET: api/Promotion/upcoming
        [HttpGet("upcoming")]
        public async Task<IActionResult> GetUpcomingPromotions()
        {
            try
            {
                var upcomingPromotions = await _promotionService.GetUpcomingPromotionsAsync();
                return Ok(upcomingPromotions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving upcoming promotions");
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách khuyến mãi sắp tới." });
            }
        }

        // POST: api/Promotion/apply-to-all
        [Authorize(Roles = "Admin")]
        [HttpPost("apply-to-all")]
        public async Task<IActionResult> ApplyPromotionToAllProducts([FromBody] ApplyPromotionToAllRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _promotionService.ApplyPromotionToAllProductsAsync(request);
                if (result)
                {
                    return Ok(new { message = "Khuyến mãi đã được áp dụng cho tất cả sản phẩm." });
                }
                return BadRequest(new { message = "Không thể áp dụng khuyến mãi cho tất cả sản phẩm." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying promotion to all products");
                return StatusCode(500, new { message = "Lỗi khi áp dụng khuyến mãi cho tất cả sản phẩm." });
            }
        }

        // GET: api/Promotion/search
        [HttpGet("search")]
        public async Task<IActionResult> SearchPromotions(
            [FromQuery] string keyword,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            try
            {
                var promotions = await _promotionService.SearchPromotionsAsync(keyword, startDate, endDate);
                return Ok(promotions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching promotions");
                return StatusCode(500, new { message = "Lỗi khi tìm kiếm khuyến mãi." });
            }
        }

        // POST: api/Promotion/apply-to-category
        [Authorize(Roles = "Admin")]
        [HttpPost("apply-to-category")]
        public async Task<IActionResult> ApplyPromotionToCategory([FromBody] ApplyPromotionToCategoryRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _promotionService.ApplyPromotionToCategoryAsync(request);
                if (result)
                {
                    return Ok(new { message = "Khuyến mãi đã được áp dụng cho các sản phẩm trong danh mục." });
                }
                return BadRequest(new { message = "Không thể áp dụng khuyến mãi cho danh mục. Có thể danh mục không có sản phẩm." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error applying promotion to category ID: {request.CategoryId}");
                return StatusCode(500, new { message = "Lỗi khi áp dụng khuyến mãi cho danh mục." });
            }
        }

        // DELETE: api/Promotion/cancel-all-active
        [Authorize(Roles = "Admin")]
        [HttpDelete("cancel-all-active")]
        public async Task<IActionResult> CancelAllActivePromotions()
        {
            try
            {
                var result = await _promotionService.CancelAllActivePromotionsAsync();
                if (result)
                {
                    return Ok(new { message = "Tất cả các khuyến mãi đang hoạt động đã bị hủy." });
                }
                return BadRequest(new { message = "Lỗi khi hủy các khuyến mãi đang hoạt động." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling all active promotions");
                return StatusCode(500, new { message = "Lỗi khi hủy tất cả các khuyến mãi đang hoạt động." });
            }
        }

        // GET: api/Promotion/statistics/{promotionId}
        [Authorize(Roles = "Admin")]
        [HttpGet("statistics/{promotionId}")]
        public async Task<IActionResult> GetPromotionStatistics(int promotionId)
        {
            try
            {
                var statistics = await _promotionService.GetPromotionStatisticsAsync(promotionId);
                if (statistics == null)
                {
                    return NotFound(new { message = "Không tìm thấy khuyến mãi." });
                }
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting statistics for promotion ID: {promotionId}");
                return StatusCode(500, new { message = "Lỗi khi lấy thống kê khuyến mãi." });
            }
        }
    }

    // Request classes - kept in the same file for backward compatibility
    public class CreatePromotionRequest
    {
        public int? ProductId { get; set; }
        public string Name { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class UpdatePromotionRequest
    {
        public int? ProductId { get; set; }
        public string Name { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class ApplyPromotionToAllRequest
    {
        public string Name { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class ApplyPromotionToCategoryRequest
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
