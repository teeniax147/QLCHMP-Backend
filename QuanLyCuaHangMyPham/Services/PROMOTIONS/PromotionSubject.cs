using QuanLyCuaHangMyPham.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuanLyCuaHangMyPham.Services.PROMOTIONS
{
    public class PromotionSubject : IPromotionSubject
    {
        private readonly List<IPromotionObserver> _observers = new List<IPromotionObserver>();
        private readonly ILogger<PromotionSubject> _logger;
        private readonly object _lock = new object(); // Đảm bảo thread-safety

        public PromotionSubject(ILogger<PromotionSubject> logger)
        {
            _logger = logger;
        }

        public void Attach(IPromotionObserver observer)
        {
            lock (_lock) // Đảm bảo thread-safety
            {
                if (!_observers.Contains(observer))
                {
                    _observers.Add(observer);
                    _logger.LogInformation($"Observer {observer.GetType().Name} attached");
                }
            }
        }

        public void Detach(IPromotionObserver observer)
        {
            lock (_lock) // Đảm bảo thread-safety
            {
                if (_observers.Contains(observer))
                {
                    _observers.Remove(observer);
                    _logger.LogInformation($"Observer {observer.GetType().Name} detached");
                }
            }
        }

        public async Task NotifyPromotionCreated(Promotion promotion)
        {
            List<IPromotionObserver> observersCopy;

            // Tạo bản sao an toàn của danh sách observers
            lock (_lock)
            {
                observersCopy = new List<IPromotionObserver>(_observers);
            }

            _logger.LogInformation($"Notifying {observersCopy.Count} observers of promotion creation: {promotion.Name}");

            foreach (var observer in observersCopy)
            {
                try
                {
                    await observer.OnPromotionCreated(promotion);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error notifying observer {observer.GetType().Name} of promotion creation");
                }
            }
        }

        public async Task NotifyPromotionUpdated(Promotion promotion)
        {
            List<IPromotionObserver> observersCopy;

            // Tạo bản sao an toàn của danh sách observers
            lock (_lock)
            {
                observersCopy = new List<IPromotionObserver>(_observers);
            }

            _logger.LogInformation($"Notifying {observersCopy.Count} observers of promotion update: {promotion.Name}");

            foreach (var observer in observersCopy)
            {
                try
                {
                    await observer.OnPromotionUpdated(promotion);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error notifying observer {observer.GetType().Name} of promotion update");
                }
            }
        }

        public async Task NotifyPromotionExpired(Promotion promotion)
        {
            List<IPromotionObserver> observersCopy;

            // Tạo bản sao an toàn của danh sách observers
            lock (_lock)
            {
                observersCopy = new List<IPromotionObserver>(_observers);
            }

            _logger.LogInformation($"Notifying {observersCopy.Count} observers of promotion expiration: {promotion.Name}");

            foreach (var observer in observersCopy)
            {
                try
                {
                    await observer.OnPromotionExpired(promotion);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error notifying observer {observer.GetType().Name} of promotion expiration");
                }
            }
        }
    }
}