namespace QuanLyCuaHangMyPham.Services.PROMOTIONS.Flyweight
{
    public class PromotionFlyweightFactory
    {
        private readonly Dictionary<string, PromotionFlyweight> _flyweights = new Dictionary<string, PromotionFlyweight>();
        private readonly ILogger<PromotionFlyweightFactory> _logger;

        public PromotionFlyweightFactory(ILogger<PromotionFlyweightFactory> logger)
        {
            _logger = logger;
        }

        public PromotionFlyweight GetPromotionFlyweight(string name, decimal? discountPercentage, DateTime? startDate, DateTime? endDate)
        {
            // Create a key based on promotion properties
            string key = $"{name}_{discountPercentage}_{startDate?.ToString("yyyyMMdd")}_{endDate?.ToString("yyyyMMdd")}";

            // Return existing flyweight if already exists
            if (_flyweights.ContainsKey(key))
            {
                _logger.LogInformation($"Reusing existing promotion flyweight: {key}");
                return _flyweights[key];
            }

            // Create new flyweight if not exists
            var flyweight = new PromotionFlyweight(name, discountPercentage, startDate, endDate);
            _flyweights.Add(key, flyweight);
            _logger.LogInformation($"Created new promotion flyweight: {key}");

            return flyweight;
        }

        public int GetFlyweightCount()
        {
            return _flyweights.Count;
        }
    }
}
