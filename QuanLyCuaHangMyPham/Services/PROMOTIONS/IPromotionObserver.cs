using QuanLyCuaHangMyPham.Models;

namespace QuanLyCuaHangMyPham.Services.PROMOTIONS
{
    public interface IPromotionObserver
    {
        Task OnPromotionCreated(Promotion promotion);
        Task OnPromotionUpdated(Promotion promotion);
        Task OnPromotionExpired(Promotion promotion);
    }
}
