using QuanLyCuaHangMyPham.Controllers;
using static QuanLyCuaHangMyPham.Controllers.PromotionController;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuanLyCuaHangMyPham.Data;
using QuanLyCuaHangMyPham.Models;
using QuanLyCuaHangMyPham.Services.PROMOTIONS.Flyweight;
using QuanLyCuaHangMyPham.Services.PROMOTIONS;
using QuanLyCuaHangMyPham.Services.PROMOTIONS.Observer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace QuanLyCuaHangMyPham.Services.PROMOTIONS
{
    public class PromotionService : IPromotionService
    {
        private readonly QuanLyCuaHangMyPhamContext _context;
        private readonly IPromotionSubject _promotionSubject;
        private readonly PromotionFlyweightFactory _flyweightFactory;
        private readonly ILogger<PromotionService> _logger;

        public PromotionService(
            QuanLyCuaHangMyPhamContext context,
            IPromotionSubject promotionSubject,
            PromotionFlyweightFactory flyweightFactory,
            ILogger<PromotionService> logger)
        {
            _context = context;
            _promotionSubject = promotionSubject;
            _flyweightFactory = flyweightFactory;
            _logger = logger;
        }

        /// <summary>
        /// Lấy tất cả khuyến mãi
        /// </summary>
        public async Task<IEnumerable<Promotion>> GetPromotionsAsync()
        {
            _logger.LogInformation("Retrieving all promotions");
            return await _context.Promotions.Include(p => p.Product).ToListAsync();
        }

        /// <summary>
        /// Lấy khuyến mãi theo ID
        /// </summary>
        public async Task<Promotion> GetPromotionByIdAsync(int id)
        {
            _logger.LogInformation($"Retrieving promotion with ID: {id}");
            return await _context.Promotions
                .Include(p => p.Product)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        /// <summary>
        /// Tạo khuyến mãi mới
        /// </summary>
        public async Task<Promotion> CreatePromotionAsync(CreatePromotionRequest request)
        {
            _logger.LogInformation($"Creating promotion: {request.Name}");

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
            await _context.SaveChangesAsync();

            // Notify observers
            await _promotionSubject.NotifyPromotionCreated(promotion);

            return promotion;
        }

        /// <summary>
        /// Cập nhật khuyến mãi hiện có
        /// </summary>
        public async Task<Promotion> UpdatePromotionAsync(int id, UpdatePromotionRequest request)
        {
            _logger.LogInformation($"Updating promotion with ID: {id}");

            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
            {
                _logger.LogWarning($"Promotion with ID {id} not found for update");
                return null;
            }

            // Lưu trạng thái cũ trước khi cập nhật
            bool wasActive = IsPromotionActive(promotion);

            promotion.ProductId = request.ProductId;
            promotion.Name = request.Name;
            promotion.DiscountPercentage = request.DiscountPercentage;
            promotion.StartDate = request.StartDate;
            promotion.EndDate = request.EndDate;

            _context.Promotions.Update(promotion);
            await _context.SaveChangesAsync();

            // Kiểm tra trạng thái mới
            bool isActive = IsPromotionActive(promotion);

            // Thông báo cho observers dựa trên thay đổi trạng thái
            if (wasActive && !isActive)
            {
                // Khuyến mãi từ active -> inactive
                await _promotionSubject.NotifyPromotionExpired(promotion);
            }
            else if (!wasActive && isActive)
            {
                // Khuyến mãi từ inactive -> active
                await _promotionSubject.NotifyPromotionCreated(promotion);
            }
            else
            {
                // Chỉ cập nhật thông tin
                await _promotionSubject.NotifyPromotionUpdated(promotion);
            }

            return promotion;
        }

        /// <summary>
        /// Xóa khuyến mãi
        /// </summary>
        public async Task<bool> DeletePromotionAsync(int id)
        {
            _logger.LogInformation($"Deleting promotion with ID: {id}");

            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
            {
                _logger.LogWarning($"Promotion with ID {id} not found for deletion");
                return false;
            }

            _context.Promotions.Remove(promotion);
            await _context.SaveChangesAsync();

            // Notify observers if promotion was active
            if (IsPromotionActive(promotion))
            {
                await _promotionSubject.NotifyPromotionExpired(promotion);
            }

            return true;
        }

        /// <summary>
        /// Lấy các khuyến mãi đang hoạt động
        /// </summary>
        public async Task<IEnumerable<Promotion>> GetActivePromotionsAsync()
        {
            _logger.LogInformation("Retrieving active promotions");

            var currentDate = DateTime.Now;
            return await _context.Promotions
                .Include(p => p.Product)
                .Where(p => p.StartDate <= currentDate && p.EndDate >= currentDate)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy khuyến mãi theo sản phẩm
        /// </summary>
        public async Task<IEnumerable<Promotion>> GetPromotionsByProductAsync(int productId)
        {
            _logger.LogInformation($"Retrieving promotions for product ID: {productId}");

            return await _context.Promotions
                .Where(p => p.ProductId == productId)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy khuyến mãi sắp tới
        /// </summary>
        public async Task<IEnumerable<Promotion>> GetUpcomingPromotionsAsync()
        {
            _logger.LogInformation("Retrieving upcoming promotions");

            var currentDate = DateTime.Now;
            return await _context.Promotions
                .Include(p => p.Product)
                .Where(p => p.StartDate > currentDate)
                .ToListAsync();
        }

        /// <summary>
        /// Áp dụng khuyến mãi cho tất cả sản phẩm
        /// </summary>
        public async Task<bool> ApplyPromotionToAllProductsAsync(ApplyPromotionToAllRequest request)
        {
            _logger.LogInformation($"Applying promotion to all products: {request.Name}");

            try
            {
                // Use flyweight pattern to create a shared promotion definition
                var sharedPromotion = _flyweightFactory.GetPromotionFlyweight(
                    request.Name,
                    request.DiscountPercentage,
                    request.StartDate,
                    request.EndDate);

                // Get all products
                var products = await _context.Products.ToListAsync();
                if (products.Count == 0)
                {
                    _logger.LogWarning("No products found to apply promotion");
                    return false;
                }

                // Create product-specific promotions using the shared flyweight
                var productPromotions = new List<Promotion>();
                foreach (var product in products)
                {
                    var productPromotion = new ProductPromotion(sharedPromotion, product.Id);
                    productPromotions.Add(productPromotion.ToPromotionEntity());
                }

                // Add all promotions to the database
                _context.Promotions.AddRange(productPromotions);
                await _context.SaveChangesAsync();

                // Notify observers for each new promotion
                foreach (var promotion in productPromotions)
                {
                    if (IsPromotionActive(promotion))
                    {
                        await _promotionSubject.NotifyPromotionCreated(promotion);
                    }
                }

                _logger.LogInformation($"Applied promotion to {products.Count} products using flyweight pattern");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying promotion to all products");
                return false;
            }
        }

        /// <summary>
        /// Áp dụng khuyến mãi cho danh mục
        /// </summary>
        public async Task<bool> ApplyPromotionToCategoryAsync(ApplyPromotionToCategoryRequest request)
        {
            _logger.LogInformation($"Applying promotion to category ID {request.CategoryId}: {request.Name}");

            try
            {
                // Use flyweight pattern
                var sharedPromotion = _flyweightFactory.GetPromotionFlyweight(
                    request.Name,
                    request.DiscountPercentage,
                    request.StartDate,
                    request.EndDate);

                // Find products in the category
                var productsInCategory = await _context.Products
                    .Where(p => p.Categories.Any(c => c.Id == request.CategoryId))
                    .ToListAsync();

                if (productsInCategory.Count == 0)
                {
                    _logger.LogWarning($"No products found in category ID {request.CategoryId}");
                    return false;
                }

                // Create product-specific promotions
                var productPromotions = new List<Promotion>();
                foreach (var product in productsInCategory)
                {
                    var productPromotion = new ProductPromotion(sharedPromotion, product.Id);
                    productPromotions.Add(productPromotion.ToPromotionEntity());
                }

                _context.Promotions.AddRange(productPromotions);
                await _context.SaveChangesAsync();

                // Notify observers for active promotions
                foreach (var promotion in productPromotions)
                {
                    if (IsPromotionActive(promotion))
                    {
                        await _promotionSubject.NotifyPromotionCreated(promotion);
                    }
                }

                _logger.LogInformation($"Applied promotion to {productsInCategory.Count} products in category ID {request.CategoryId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error applying promotion to category ID {request.CategoryId}");
                return false;
            }
        }

        /// <summary>
        /// Hủy tất cả khuyến mãi đang hoạt động
        /// </summary>
        public async Task<bool> CancelAllActivePromotionsAsync()
        {
            _logger.LogInformation("Cancelling all active promotions");

            try
            {
                var currentDate = DateTime.Now;
                var activePromotions = await _context.Promotions
                    .Where(p => p.StartDate <= currentDate && p.EndDate >= currentDate)
                    .ToListAsync();

                if (activePromotions.Count == 0)
                {
                    _logger.LogInformation("No active promotions found to cancel");
                    return true; // Không có gì để hủy, vẫn trả về thành công
                }

                foreach (var promotion in activePromotions)
                {
                    _context.Promotions.Remove(promotion);

                    // Thông báo cho observers
                    await _promotionSubject.NotifyPromotionExpired(promotion);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Cancelled {activePromotions.Count} active promotions");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling all active promotions");
                return false;
            }
        }

        /// <summary>
        /// Lấy thống kê khuyến mãi
        /// </summary>
        public async Task<object> GetPromotionStatisticsAsync(int promotionId)
        {
            _logger.LogInformation($"Getting statistics for promotion ID: {promotionId}");

            try
            {
                var promotion = await _context.Promotions
                    .Include(p => p.Product)
                    .FirstOrDefaultAsync(p => p.Id == promotionId);

                if (promotion == null)
                {
                    _logger.LogWarning($"Promotion with ID {promotionId} not found for statistics");
                    return null;
                }

                // Số lượng đơn hàng sử dụng khuyến mãi này
                var ordersWithPromotion = await _context.Orders
                    .Where(o => o.CouponId == promotionId)
                    .ToListAsync();

                var totalOrders = ordersWithPromotion.Count;
                var totalRevenue = ordersWithPromotion.Sum(o => o.TotalAmount ?? 0);
                var averageDiscount = ordersWithPromotion.Any()
                    ? ordersWithPromotion.Average(o => o.DiscountApplied ?? 0)
                    : 0;

                // Thống kê thêm: số lượng sản phẩm bán được
                var totalProductsSold = await _context.OrderDetails
                    .Where(od => ordersWithPromotion.Select(o => o.Id).Contains(od.OrderId))
                    .SumAsync(od => od.Quantity ?? 0);

                // Nếu là sản phẩm cụ thể, tính tổng doanh thu cho sản phẩm đó
                decimal productRevenue = 0;
                if (promotion.ProductId.HasValue)
                {
                    productRevenue = await _context.OrderDetails
                        .Where(od => ordersWithPromotion.Select(o => o.Id).Contains(od.OrderId)
                               && od.ProductId == promotion.ProductId.Value)
                        .SumAsync(od => od.TotalPrice ?? 0);
                }

                return new
                {
                    PromotionId = promotion.Id,
                    PromotionName = promotion.Name,
                    ProductName = promotion.Product?.Name,
                    DiscountPercentage = promotion.DiscountPercentage,
                    StartDate = promotion.StartDate,
                    EndDate = promotion.EndDate,
                    TotalOrders = totalOrders,
                    TotalRevenue = totalRevenue,
                    AverageDiscount = averageDiscount,
                    TotalProductsSold = totalProductsSold,
                    ProductRevenue = productRevenue,
                    Status = IsPromotionActive(promotion) ? "Đang hoạt động" :
                             (promotion.StartDate > DateTime.Now ? "Sắp diễn ra" : "Đã kết thúc")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting statistics for promotion ID {promotionId}");
                return null;
            }
        }

        /// <summary>
        /// Tìm kiếm khuyến mãi
        /// </summary>
        public async Task<IEnumerable<Promotion>> SearchPromotionsAsync(string keyword, DateTime? startDate, DateTime? endDate)
        {
            _logger.LogInformation($"Searching promotions with keyword: {keyword}, startDate: {startDate}, endDate: {endDate}");

            var query = _context.Promotions.Include(p => p.Product).AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(keyword) ||
                    (p.Product != null && p.Product.Name.ToLower().Contains(keyword))
                );
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
            _logger.LogInformation($"Found {promotions.Count} promotions matching criteria");

            return promotions;
        }

        /// <summary>
        /// Kiểm tra khuyến mãi có đang hoạt động không
        /// </summary>
        private bool IsPromotionActive(Promotion promotion)
        {
            var currentDate = DateTime.Now;
            return promotion.StartDate <= currentDate && promotion.EndDate >= currentDate;
        }
    }
}
