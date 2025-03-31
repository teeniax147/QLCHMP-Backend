namespace QuanLyCuaHangMyPham.Services.PROMOTIONS.Flyweight
{
    public class PromotionFlyweight
    {
        // Intrinsic state - shared across products
        public string Name { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime CreatedAt { get; set; }

        public PromotionFlyweight(string name, decimal? discountPercentage, DateTime? startDate, DateTime? endDate)
        {
            Name = name;
            DiscountPercentage = discountPercentage;
            StartDate = startDate;
            EndDate = endDate;
            CreatedAt = DateTime.Now;
        }
    }
}
