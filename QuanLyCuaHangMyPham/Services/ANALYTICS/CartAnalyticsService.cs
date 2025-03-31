// CartAnalyticsService.cs
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QuanLyCuaHangMyPham.Mediators;

namespace QuanLyCuaHangMyPham.Services.Analytics
{
    public class CartAnalyticsService : IColleague
    {
        private readonly ILogger<CartAnalyticsService> _logger;

        public IMediator Mediator { get; set; }

        public CartAnalyticsService(ILogger<CartAnalyticsService> logger)
        {
            _logger = logger;
        }

        public async Task ReceiveCartNotification(int userId, string action, int? productId, int? quantity)
        {
            // Ghi lại hoạt động giỏ hàng cho analytics
            _logger.LogInformation($"Analytics: User {userId} {action} product {productId}, quantity {quantity}");

            await LogCartActivity(userId, action, productId, quantity);
        }

        private async Task LogCartActivity(int userId, string action, int? productId, int? quantity)
        {
            // Ghi log vào hệ thống analytics
            await Task.CompletedTask;
        }
    }
}