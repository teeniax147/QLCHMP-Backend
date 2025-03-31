using QuanLyCuaHangMyPham.Models;

namespace QuanLyCuaHangMyPham.Services.PROMOTIONS
{
    public class PromotionSubject : IPromotionSubject
    {
        private readonly List<IPromotionObserver> _observers = new List<IPromotionObserver>();
        private readonly ILogger<PromotionSubject> _logger;

        public PromotionSubject(ILogger<PromotionSubject> logger)
        {
            _logger = logger;
        }

        public void Attach(IPromotionObserver observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
                _logger.LogInformation($"Observer {observer.GetType().Name} attached");
            }
        }

        public void Detach(IPromotionObserver observer)
        {
            if (_observers.Contains(observer))
            {
                _observers.Remove(observer);
                _logger.LogInformation($"Observer {observer.GetType().Name} detached");
            }
        }

        public async Task NotifyPromotionCreated(Promotion promotion)
        {
            _logger.LogInformation($"Notifying {_observers.Count} observers of promotion creation: {promotion.Name}");
            foreach (var observer in _observers)
            {
                await observer.OnPromotionCreated(promotion);
            }
        }

        public async Task NotifyPromotionUpdated(Promotion promotion)
        {
            _logger.LogInformation($"Notifying {_observers.Count} observers of promotion update: {promotion.Name}");
            foreach (var observer in _observers)
            {
                await observer.OnPromotionUpdated(promotion);
            }
        }

        public async Task NotifyPromotionExpired(Promotion promotion)
        {
            _logger.LogInformation($"Notifying {_observers.Count} observers of promotion expiration: {promotion.Name}");
            foreach (var observer in _observers)
            {
                await observer.OnPromotionExpired(promotion);
            }
        }
    }
}
