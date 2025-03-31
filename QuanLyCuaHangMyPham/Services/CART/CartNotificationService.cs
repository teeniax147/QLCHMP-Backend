// CartNotificationService.cs
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QuanLyCuaHangMyPham.Mediators;

namespace QuanLyCuaHangMyPham.Services.Cart
{
    public class CartNotificationService : IColleague
    {
        private readonly ILogger<CartNotificationService> _logger;

        public IMediator Mediator { get; set; }

        public CartNotificationService(ILogger<CartNotificationService> logger)
        {
            _logger = logger;
        }

        public async Task ReceiveCartNotification(int userId, string action, int? productId, int? quantity)
        {
            _logger.LogInformation($"Cart notification: User {userId} {action} product {productId}, quantity {quantity}");

            // Ví dụ về xử lý thông báo
            switch (action)
            {
                case "Add":
                    await NotifyCartItemAdded(userId, productId, quantity);
                    break;
                case "Remove":
                    await NotifyCartItemRemoved(userId, productId, quantity);
                    break;
                case "Update":
                    await NotifyCartItemUpdated(userId, productId, quantity);
                    break;
                case "Clear":
                    await NotifyCartCleared(userId);
                    break;
            }
        }

        private async Task NotifyCartItemAdded(int userId, int? productId, int? quantity)
        {
            // Gửi thông báo khi thêm sản phẩm
            await Task.CompletedTask;
        }

        private async Task NotifyCartItemRemoved(int userId, int? productId, int? quantity)
        {
            // Gửi thông báo khi xóa sản phẩm
            await Task.CompletedTask;
        }

        private async Task NotifyCartItemUpdated(int userId, int? productId, int? quantity)
        {
            // Gửi thông báo khi cập nhật sản phẩm
            await Task.CompletedTask;
        }

        private async Task NotifyCartCleared(int userId)
        {
            // Gửi thông báo khi xóa giỏ hàng
            await Task.CompletedTask;
        }
    }
}