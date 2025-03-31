using QuanLyCuaHangMyPham.Controllers;
using QuanLyCuaHangMyPham.Models;
using static QuanLyCuaHangMyPham.Controllers.PromotionController;

namespace QuanLyCuaHangMyPham.Services.PROMOTIONS
{
    public interface IPromotionService
    {
        Task<IEnumerable<Promotion>> GetPromotionsAsync();
        Task<Promotion> GetPromotionByIdAsync(int id);
        Task<Promotion> CreatePromotionAsync(CreatePromotionRequest request);
        Task<Promotion> UpdatePromotionAsync(int id, UpdatePromotionRequest request);
        Task<bool> DeletePromotionAsync(int id);
        Task<IEnumerable<Promotion>> GetActivePromotionsAsync();
        Task<IEnumerable<Promotion>> GetPromotionsByProductAsync(int productId);
        Task<IEnumerable<Promotion>> GetUpcomingPromotionsAsync();
        Task<bool> ApplyPromotionToAllProductsAsync(ApplyPromotionToAllRequest request);
        Task<bool> ApplyPromotionToCategoryAsync(ApplyPromotionToCategoryRequest request);
        Task<bool> CancelAllActivePromotionsAsync();
        Task<object> GetPromotionStatisticsAsync(int promotionId);
        Task<IEnumerable<Promotion>> SearchPromotionsAsync(string keyword, DateTime? startDate, DateTime? endDate);
    }
}
