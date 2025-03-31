using Microsoft.Extensions.Logging;
using QuanLyCuaHangMyPham.Data;
using QuanLyCuaHangMyPham.Models;

namespace QuanLyCuaHangMyPham.States.OrderStates
{
    public class DeliveredOrderState : OrderStateBase
    {
        public override string StateName => "Đã Giao";

        public DeliveredOrderState(QuanLyCuaHangMyPhamContext context, ILogger<OrderStateBase> logger)
            : base(context, logger)
        {
        }

        // Đơn hàng đã giao không thể chuyển trạng thái
    }
}