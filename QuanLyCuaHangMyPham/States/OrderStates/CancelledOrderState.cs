using Microsoft.Extensions.Logging;
using QuanLyCuaHangMyPham.Data;
using QuanLyCuaHangMyPham.Models;

namespace QuanLyCuaHangMyPham.States.OrderStates
{
    public class CancelledOrderState : OrderStateBase
    {
        public override string StateName => "Đã Hủy";

        public CancelledOrderState(QuanLyCuaHangMyPhamContext context, ILogger<OrderStateBase> logger)
            : base(context, logger)
        {
        }

        // Đơn hàng đã hủy không thể chuyển trạng thái
    }
}