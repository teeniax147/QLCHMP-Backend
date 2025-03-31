using Microsoft.Extensions.Logging;
using QuanLyCuaHangMyPham.Data;
using QuanLyCuaHangMyPham.Models;
using QuanLyCuaHangMyPham.States.OrderStates;
using System.Threading.Tasks;

namespace QuanLyCuaHangMyPham.States
{
    public class OrderStateContext
    {
        private readonly QuanLyCuaHangMyPhamContext _context;
        private readonly ILogger<OrderStateBase> _logger;
        private IOrderState _state;

        public OrderStateContext(QuanLyCuaHangMyPhamContext context, ILogger<OrderStateBase> logger)
        {
            _context = context;
            _logger = logger;
        }

        public void SetState(Order order)
        {
            _state = order.Status switch
            {
                "Chờ Xác Nhận" => new PendingOrderState(_context, _logger),
                "Chờ Lấy Hàng" => new ReadyToShipOrderState(_context, _logger),
                "Đang Giao Hàng" => new ShippingOrderState(_context, _logger),
                "Đã Giao" => new DeliveredOrderState(_context, _logger),
                "Đã Hủy" => new CancelledOrderState(_context, _logger),
                _ => new PendingOrderState(_context, _logger)
            };
        }

        public async Task<bool> ConfirmOrder(Order order)
        {
            SetState(order);
            return await _state.ConfirmOrder(order);
        }

        public async Task<bool> ReadyToShip(Order order)
        {
            SetState(order);
            return await _state.ReadyToShip(order);
        }

        public async Task<bool> Ship(Order order)
        {
            SetState(order);
            return await _state.Ship(order);
        }

        public async Task<bool> Deliver(Order order)
        {
            SetState(order);
            return await _state.Deliver(order);
        }

        public async Task<bool> Cancel(Order order, string reason)
        {
            SetState(order);
            return await _state.Cancel(order, reason);
        }

        public bool CanConfirm(Order order)
        {
            SetState(order);
            return _state.CanConfirm();
        }

        public bool CanShip(Order order)
        {
            SetState(order);
            return _state.CanShip();
        }

        public bool CanDeliver(Order order)
        {
            SetState(order);
            return _state.CanDeliver();
        }

        public bool CanCancel(Order order)
        {
            SetState(order);
            return _state.CanCancel();
        }
    }
}