using System.Threading.Tasks;

namespace QuanLyCuaHangMyPham.Mediators
{
    public interface IColleague
    {
        // Thiết lập mediator
        IMediator Mediator { get; set; }

        // Nhận thông báo giỏ hàng thay đổi từ mediator
        Task ReceiveCartNotification(int userId, string action, int? productId, int? quantity);
    }
}