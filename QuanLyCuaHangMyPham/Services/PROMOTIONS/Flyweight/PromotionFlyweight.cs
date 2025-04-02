using System;

namespace QuanLyCuaHangMyPham.Services.PROMOTIONS.Flyweight
{
    public class PromotionFlyweight
    {
        // Trạng thái nội tại - được chia sẻ giữa các sản phẩm
        // Làm cho thuộc tính chỉ đọc để đảm bảo tính bất biến (immutability)
        public string Name { get; }
        public decimal? DiscountPercentage { get; }
        public DateTime? StartDate { get; }
        public DateTime? EndDate { get; }
        public DateTime CreatedAt { get; }

        public PromotionFlyweight(string name, decimal? discountPercentage, DateTime? startDate, DateTime? endDate)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            DiscountPercentage = discountPercentage;
            StartDate = startDate;
            EndDate = endDate;
            CreatedAt = DateTime.Now;
        }

        // Thêm các phương thức tiện ích
        public bool IsActive(DateTime currentDate)
        {
            return StartDate.HasValue && EndDate.HasValue &&
                   currentDate >= StartDate.Value && currentDate <= EndDate.Value;
        }

        public decimal CalculateDiscountedPrice(decimal originalPrice)
        {
            if (!DiscountPercentage.HasValue) return originalPrice;

            return originalPrice * (1 - (DiscountPercentage.Value / 100));
        }
    }
}