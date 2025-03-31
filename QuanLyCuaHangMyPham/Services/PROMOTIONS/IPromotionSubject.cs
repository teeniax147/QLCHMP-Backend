using QuanLyCuaHangMyPham.Models;

namespace QuanLyCuaHangMyPham.Services.PROMOTIONS
{
    public interface IPromotionSubject
    {
        void Attach(IPromotionObserver observer);
        void Detach(IPromotionObserver observer);
        Task NotifyPromotionCreated(Promotion promotion);
        Task NotifyPromotionUpdated(Promotion promotion);
        Task NotifyPromotionExpired(Promotion promotion);
    }
}
