using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuanLyCuaHangMyPham.Data;
using QuanLyCuaHangMyPham.Models;
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
        private readonly QuanLyCuaHangMyPhamContext _context;
        private readonly ILogger<PromotionController> _logger;

        public PromotionController(QuanLyCuaHangMyPhamContext context, ILogger<PromotionController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Promotion
        [HttpGet]
        public async Task<IActionResult> GetPromotions()
        {
            var promotions = await _context.Promotions.Include(p => p.Product).ToListAsync();
            return Ok(promotions);
        }

        // GET: api/Promotion/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPromotion(int id)
        {
            var promotion = await _context.Promotions.Include(p => p.Product).FirstOrDefaultAsync(p => p.Id == id);
            if (promotion == null)
            {
                return NotFound(new { message = "Không tìm thấy khuyến mãi." });
            }
            return Ok(promotion);
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

            var promotion = new Promotion
            {
                ProductId = request.ProductId,
                Name = request.Name,
                DiscountPercentage = request.DiscountPercentage,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                CreatedAt = DateTime.Now
            };

            _context.Promotions.Add(promotion);

            // Cập nhật giá sản phẩm nếu khuyến mãi mới đang có hiệu lực
            if (request.StartDate <= DateTime.Now && request.EndDate >= DateTime.Now)
            {
                var product = await _context.Products.FindAsync(request.ProductId);
                if (product != null)
                {
                    product.Price = product.OriginalPrice * (1 - (request.DiscountPercentage ?? 0) / 100);
                    _context.Products.Update(product);
                }
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPromotion), new { id = promotion.Id }, promotion);
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

            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
            {
                return NotFound(new { message = "Không tìm thấy khuyến mãi." });
            }

            promotion.ProductId = request.ProductId;
            promotion.Name = request.Name;
            promotion.DiscountPercentage = request.DiscountPercentage;
            promotion.StartDate = request.StartDate;
            promotion.EndDate = request.EndDate;

            _context.Promotions.Update(promotion);

            // Cập nhật giá sản phẩm nếu khuyến mãi được chỉnh sửa đang có hiệu lực
            var product = await _context.Products.FindAsync(request.ProductId);
            if (product != null)
            {
                if (request.StartDate <= DateTime.Now && request.EndDate >= DateTime.Now)
                {
                    product.Price = product.OriginalPrice * (1 - (request.DiscountPercentage ?? 0) / 100);
                }
                else
                {
                    product.Price = product.OriginalPrice; // Hết hiệu lực khuyến mãi, quay lại giá gốc
                }
                _context.Products.Update(product);
            }

            await _context.SaveChangesAsync();

            return Ok(promotion);
        }

        // DELETE: api/Promotion/{id}
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePromotion(int id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
            {
                return NotFound(new { message = "Không tìm thấy khuyến mãi." });
            }

            _context.Promotions.Remove(promotion);

            // Cập nhật giá sản phẩm nếu khuyến mãi bị xóa
            var product = await _context.Products.FindAsync(promotion.ProductId);
            if (product != null)
            {
                product.Price = product.OriginalPrice;
                _context.Products.Update(product);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Khuyến mãi đã được xóa thành công." });
        }

        // GET: api/Promotion/active
        [HttpGet("active")]
        public async Task<IActionResult> GetActivePromotions()
        {
            var currentDate = DateTime.Now;
            var activePromotions = await _context.Promotions
                .Include(p => p.Product)
                .Where(p => p.StartDate <= currentDate && p.EndDate >= currentDate)
                .ToListAsync();
            return Ok(activePromotions);
        }

        // GET: api/Promotion/product/{productId}
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetPromotionsByProduct(int productId)
        {
            var promotions = await _context.Promotions
                .Where(p => p.ProductId == productId)
                .ToListAsync();
            return Ok(promotions);
        }

        // GET: api/Promotion/upcoming
        [HttpGet("upcoming")]
        public async Task<IActionResult> GetUpcomingPromotions()
        {
            var currentDate = DateTime.Now;
            var upcomingPromotions = await _context.Promotions
                .Include(p => p.Product)
                .Where(p => p.StartDate > currentDate)
                .ToListAsync();
            return Ok(upcomingPromotions);
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

            var products = await _context.Products.ToListAsync();
            foreach (var product in products)
            {
                var promotion = new Promotion
                {
                    ProductId = product.Id,
                    Name = request.Name,
                    DiscountPercentage = request.DiscountPercentage,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    CreatedAt = DateTime.Now
                };
                _context.Promotions.Add(promotion);

                // Cập nhật giá sản phẩm nếu khuyến mãi mới đang có hiệu lực
                if (request.StartDate <= DateTime.Now && request.EndDate >= DateTime.Now)
                {
                    product.Price = product.OriginalPrice * (1 - (request.DiscountPercentage ?? 0) / 100);
                    _context.Products.Update(product);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Khuyến mãi đã được áp dụng cho tất cả sản phẩm." });
        }
        //API để tìm kiếm khuyến mãi theo tên hoặc thời gian
        [HttpGet("search")]
        public async Task<IActionResult> SearchPromotions([FromQuery] string keyword, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var query = _context.Promotions.Include(p => p.Product).AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(p => p.Name.Contains(keyword));
            }

            if (startDate.HasValue)
            {
                query = query.Where(p => p.StartDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(p => p.EndDate <= endDate.Value);
            }

            var promotions = await query.ToListAsync();
            return Ok(promotions);
        }
        //API để áp dụng một khuyến mãi cho danh mục sản phẩm cụ thể
        [Authorize(Roles = "Admin")]
        [HttpPost("apply-to-category")]
        public async Task<IActionResult> ApplyPromotionToCategory([FromBody] ApplyPromotionToCategoryRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var productsInCategory = await _context.Products
                .Where(p => p.Categories.Any(c => c.Id == request.CategoryId))
                .ToListAsync();

            foreach (var product in productsInCategory)
            {
                var promotion = new Promotion
                {
                    ProductId = product.Id,
                    Name = request.Name,
                    DiscountPercentage = request.DiscountPercentage,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    CreatedAt = DateTime.Now
                };
                _context.Promotions.Add(promotion);

                // Cập nhật giá sản phẩm nếu khuyến mãi mới đang có hiệu lực
                if (request.StartDate <= DateTime.Now && request.EndDate >= DateTime.Now)
                {
                    product.Price = product.OriginalPrice * (1 - (request.DiscountPercentage ?? 0) / 100);
                    _context.Products.Update(product);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Khuyến mãi đã được áp dụng cho các sản phẩm trong danh mục." });
        }

        //API để hủy tất cả các khuyến mãi hiện tại
        [Authorize(Roles = "Admin")]
        [HttpDelete("cancel-all-active")]
        public async Task<IActionResult> CancelAllActivePromotions()
        {
            var currentDate = DateTime.Now;
            var activePromotions = await _context.Promotions
                .Where(p => p.StartDate <= currentDate && p.EndDate >= currentDate)
                .ToListAsync();

            foreach (var promotion in activePromotions)
            {
                var product = await _context.Products.FindAsync(promotion.ProductId);
                if (product != null)
                {
                    product.Price = product.OriginalPrice;
                    _context.Products.Update(product);
                }

                _context.Promotions.Remove(promotion);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Tất cả các khuyến mãi đang hoạt động đã bị hủy." });
        }

        //API để thống kê hiệu quả của khuyến mãi
        [Authorize(Roles = "Admin")]
        [HttpGet("statistics/{promotionId}")]
        public async Task<IActionResult> GetPromotionStatistics(int promotionId)
        {
            var promotion = await _context.Promotions.Include(p => p.Product).FirstOrDefaultAsync(p => p.Id == promotionId);
            if (promotion == null)
            {
                return NotFound(new { message = "Không tìm thấy khuyến mãi." });
            }

            var ordersWithPromotion = await _context.Orders
                .Where(o => o.CouponId == promotionId)
                .ToListAsync();

            var totalOrders = ordersWithPromotion.Count;
            var totalRevenue = ordersWithPromotion.Sum(o => o.TotalAmount);

            return Ok(new
            {
                PromotionName = promotion.Name,
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue
            });
        }
      
        //

        public class ApplyPromotionToCategoryRequest
        {
            public int CategoryId { get; set; }
            public string Name { get; set; }
            public decimal? DiscountPercentage { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
        }


    }

    // Request classes
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
}
