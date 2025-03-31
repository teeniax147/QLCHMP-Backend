// CartMediatorConfigurator.cs
using Microsoft.Extensions.DependencyInjection;
using QuanLyCuaHangMyPham.Mediators;
using QuanLyCuaHangMyPham.Services.Analytics;
using QuanLyCuaHangMyPham.Services.Cart;

namespace QuanLyCuaHangMyPham.Mediators.Cart
{
    public class CartMediatorConfigurator
    {
        private readonly IMediator _mediator;
        private readonly CartNotificationService _notificationService;
        private readonly CartAnalyticsService _analyticsService;

        public CartMediatorConfigurator(
            IMediator mediator,
            CartNotificationService notificationService,
            CartAnalyticsService analyticsService)
        {
            _mediator = mediator;
            _notificationService = notificationService;
            _analyticsService = analyticsService;

            // Đăng ký các colleague với mediator
            _mediator.RegisterColleague(_notificationService);
            _mediator.RegisterColleague(_analyticsService);
        }
    }
}
