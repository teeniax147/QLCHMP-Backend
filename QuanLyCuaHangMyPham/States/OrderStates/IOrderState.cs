using QuanLyCuaHangMyPham.Models;
using System.Threading.Tasks;

namespace QuanLyCuaHangMyPham.States.OrderStates
{
    public interface IOrderState
    {
        string StateName { get; }

        // Các phương thức chuyển trạng thái
        Task<bool> ConfirmOrder(Order order);
        Task<bool> ReadyToShip(Order order);
        Task<bool> Ship(Order order);
        Task<bool> Deliver(Order order);
        Task<bool> Cancel(Order order, string reason);

        // Các phương thức kiểm tra
        bool CanConfirm();
        bool CanReadyToShip();
        bool CanShip();
        bool CanDeliver();
        bool CanCancel();
    }
}