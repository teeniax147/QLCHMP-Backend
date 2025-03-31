using Microsoft.Extensions.Logging;
using QuanLyCuaHangMyPham.Data;
using QuanLyCuaHangMyPham.Models;
using System.Threading.Tasks;

namespace QuanLyCuaHangMyPham.States.OrderStates
{
    public class ShippingOrderState : OrderStateBase
    {
        public override string StateName => "Đang Giao Hàng";

        public ShippingOrderState(QuanLyCuaHangMyPhamContext context, ILogger<OrderStateBase> logger)
            : base(context, logger)
        {
        }

        public override async Task<bool> Deliver(Order order)
        {
            order.Status = "Đã Giao";
            order.PaymentStatus = "Đã Thanh Toán";
            return await SaveOrderChanges(order);
        }

        public override bool CanDeliver() => true;
    }
}