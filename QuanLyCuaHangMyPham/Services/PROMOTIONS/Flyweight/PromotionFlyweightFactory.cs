using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace QuanLyCuaHangMyPham.Services.PROMOTIONS.Flyweight
{
    public class PromotionFlyweightFactory
    {
        private readonly Dictionary<string, PromotionFlyweight> _flyweights = new Dictionary<string, PromotionFlyweight>();
        private readonly ILogger<PromotionFlyweightFactory> _logger;
        private readonly object _lock = new object(); // Thêm lock để đảm bảo thread-safety

        public PromotionFlyweightFactory(ILogger<PromotionFlyweightFactory> logger)
        {
            _logger = logger;
        }

        public PromotionFlyweight GetPromotionFlyweight(string name, decimal? discountPercentage, DateTime? startDate, DateTime? endDate)
        {
            // Tạo key dựa trên các thuộc tính khuyến mãi
            string key = $"{name}_{discountPercentage}_{startDate?.ToString("yyyyMMdd")}_{endDate?.ToString("yyyyMMdd")}";

            lock (_lock) // Đảm bảo thread-safety
            {
                // Trả về flyweight hiện có nếu đã tồn tại
                if (_flyweights.ContainsKey(key))
                {
                    _logger.LogInformation($"Reusing existing promotion flyweight: {key}");
                    return _flyweights[key];
                }

                // Tạo flyweight mới nếu chưa tồn tại
                var flyweight = new PromotionFlyweight(name, discountPercentage, startDate, endDate);
                _flyweights.Add(key, flyweight);
                _logger.LogInformation($"Created new promotion flyweight: {key}");
                return flyweight;
            }
        }

        public int GetFlyweightCount()
        {
            lock (_lock) // Đảm bảo thread-safety
            {
                return _flyweights.Count;
            }
        }

        // Thêm phương thức để xóa các flyweight hết hạn
        public void CleanupExpiredFlyweights()
        {
            var currentDate = DateTime.Now;
            lock (_lock)
            {
                var keysToRemove = new List<string>();

                foreach (var pair in _flyweights)
                {
                    if (pair.Value.EndDate.HasValue && pair.Value.EndDate.Value < currentDate)
                    {
                        keysToRemove.Add(pair.Key);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    _flyweights.Remove(key);
                    _logger.LogInformation($"Removed expired flyweight: {key}");
                }

                _logger.LogInformation($"Cleaned up {keysToRemove.Count} expired flyweights. Remaining: {_flyweights.Count}");
            }
        }
    }
}