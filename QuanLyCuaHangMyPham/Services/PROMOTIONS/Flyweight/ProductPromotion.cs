using QuanLyCuaHangMyPham.Models;

namespace QuanLyCuaHangMyPham.Services.PROMOTIONS.Flyweight
{
    public class ProductPromotion
    {
        // Reference to shared flyweight
        public PromotionFlyweight SharedPromotion { get; private set; }

        // Extrinsic state - specific to each product
        public int ProductId { get; private set; }

        public ProductPromotion(PromotionFlyweight sharedPromotion, int productId)
        {
            SharedPromotion = sharedPromotion;
            ProductId = productId;
        }

        // Creates a database Promotion entity from this flyweight
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
    }
}
