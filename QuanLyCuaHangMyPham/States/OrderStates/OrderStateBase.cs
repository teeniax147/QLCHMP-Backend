using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuanLyCuaHangMyPham.Data;
using QuanLyCuaHangMyPham.Models;
using QuanLyCuaHangMyPham.States.OrderStates;
using System;
using System.Threading.Tasks;

namespace QuanLyCuaHangMyPham.States.OrderStates
{
    public abstract class OrderStateBase : IOrderState
    {
        protected readonly QuanLyCuaHangMyPhamContext _context;
        protected readonly ILogger<OrderStateBase> _logger;

        public abstract string StateName { get; }

        public OrderStateBase(QuanLyCuaHangMyPhamContext context, ILogger<OrderStateBase> logger)
        {
            _context = context;
            _logger = logger;
        }

        public virtual Task<bool> ConfirmOrder(Order order)
        {
            _logger.LogWarning($"Không thể xác nhận đơn hàng ID {order.Id} trong trạng thái {StateName}");
            return Task.FromResult(false);
        }

        public virtual Task<bool> ReadyToShip(Order order)
        {
            _logger.LogWarning($"Không thể chuẩn bị giao đơn hàng ID {order.Id} trong trạng thái {StateName}");
            return Task.FromResult(false);
        }

        public virtual Task<bool> Ship(Order order)
        {
            _logger.LogWarning($"Không thể giao đơn hàng ID {order.Id} trong trạng thái {StateName}");
            return Task.FromResult(false);
        }

        public virtual Task<bool> Deliver(Order order)
        {
            _logger.LogWarning($"Không thể xác nhận đã giao đơn hàng ID {order.Id} trong trạng thái {StateName}");
            return Task.FromResult(false);
        }

        public virtual Task<bool> Cancel(Order order, string reason)
        {
            _logger.LogWarning($"Không thể hủy đơn hàng ID {order.Id} trong trạng thái {StateName}");
            return Task.FromResult(false);
        }

        public virtual bool CanConfirm() => false;
        public virtual bool CanReadyToShip() => false;
        public virtual bool CanShip() => false;
        public virtual bool CanDeliver() => false;
        public virtual bool CanCancel() => false;

        protected async Task<bool> SaveOrderChanges(Order order)
        {
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Đơn hàng ID {order.Id} đã chuyển sang trạng thái {order.Status}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi cập nhật trạng thái đơn hàng ID {order.Id}");
                return false;
            }
        }
    }
}