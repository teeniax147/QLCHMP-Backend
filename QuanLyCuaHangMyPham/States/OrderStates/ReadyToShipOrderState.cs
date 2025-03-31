using Microsoft.Extensions.Logging;
using QuanLyCuaHangMyPham.Data;
using QuanLyCuaHangMyPham.Models;
using System.Threading.Tasks;

namespace QuanLyCuaHangMyPham.States.OrderStates
{
    public class ReadyToShipOrderState : OrderStateBase
    {
        public override string StateName => "Chờ Lấy Hàng";

        public ReadyToShipOrderState(QuanLyCuaHangMyPhamContext context, ILogger<OrderStateBase> logger)
            : base(context, logger)
        {
        }

        public override async Task<bool> Ship(Order order)
        {
            order.Status = "Đang Giao Hàng";
            return await SaveOrderChanges(order);
        }

        public override async Task<bool> Cancel(Order order, string reason)
        {
            order.Status = "Đã Hủy";
            order.PaymentStatus = "Đã Hủy. Sẽ hoàn tiền trong 24h đối với giao dịch chuyển khoản";
            order.OrderNotes = reason;
            return await SaveOrderChanges(order);
        }

        public override bool CanShip() => true;
        public override bool CanCancel() => true;
    }
}