using System;
using QuanLyCuaHangMyPham.Models;

namespace QuanLyCuaHangMyPham.Services.PROMOTIONS.Flyweight
{
    public class ProductPromotion
    {
        // Reference to shared flyweight
        public PromotionFlyweight SharedPromotion { get; }

        // Extrinsic state - specific to each product
        public int ProductId { get; }

        public ProductPromotion(PromotionFlyweight sharedPromotion, int productId)
        {
            SharedPromotion = sharedPromotion ?? throw new ArgumentNullException(nameof(sharedPromotion));
            ProductId = productId;
        }

        // Tạo đối tượng Promotion từ flyweight để lưu vào database
        public Promotion ToPromotionEntity()
        {
            return new Promotion
            {
                ProductId = ProductId,
                Name = SharedPromotion.Name,
                DiscountPercentage = SharedPromotion.DiscountPercentage,
                StartDate = SharedPromotion.StartDate,
                EndDate = SharedPromotion.EndDate,
                CreatedAt = SharedPromotion.CreatedAt
            };
        }

        // Kiểm tra khuyến mãi có đang hoạt động không
        public bool IsActive()
        {
            return SharedPromotion.IsActive(DateTime.Now);
        }

        // Tính giá sau khuyến mãi
        public decimal GetDiscountedPrice(decimal originalPrice)
        {
            return SharedPromotion.CalculateDiscountedPrice(originalPrice);
        }
    }
}