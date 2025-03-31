using Microsoft.EntityFrameworkCore;
using QuanLyCuaHangMyPham.Data;
using QuanLyCuaHangMyPham.Models;

namespace QuanLyCuaHangMyPham.Services.PROMOTIONS.Observer
{
    public class ProductPriceUpdater : IPromotionObserver
{
    private readonly QuanLyCuaHangMyPhamContext _context;
    private readonly ILogger<ProductPriceUpdater> _logger;

    public ProductPriceUpdater(QuanLyCuaHangMyPhamContext context, ILogger<ProductPriceUpdater> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task OnPromotionCreated(Promotion promotion)
    {
        await UpdateProductPrice(promotion);
    }

    public async Task OnPromotionUpdated(Promotion promotion)
    {
        await UpdateProductPrice(promotion);
    }

    public async Task OnPromotionExpired(Promotion promotion)
    {
        // Khi khuyến mãi hết hạn, khôi phục giá gốc
        if (promotion.ProductId.HasValue)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == promotion.ProductId.Value && p.CurrentPromotionId == promotion.Id);
                
            if (product != null)
            {
                // Đặt lại giá
                product.Price = product.OriginalPrice;
                product.CurrentShockPrice = null;
                product.CurrentPromotionId = null;
                
                _context.Products.Update(product);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Reset price for product {product.Id} after promotion expiration");
            }
        }
    }

    private async Task UpdateProductPrice(Promotion promotion)
    {
        if (!promotion.ProductId.HasValue || 
            !IsPromotionActive(promotion)) return;

        var product = await _context.Products.FindAsync(promotion.ProductId.Value);
        if (product == null) return;

        // Tính giá khuyến mãi
        decimal discountedPrice = product.OriginalPrice  * (1 - (promotion.DiscountPercentage ?? 0) / 100);
        
        // Cập nhật cả Price và CurrentShockPrice
        product.Price = discountedPrice;
        product.CurrentShockPrice = discountedPrice;
        product.CurrentPromotionId = promotion.Id;
        
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Updated price for product {product.Id} with promotion {promotion.Name}");
    }

    private bool IsPromotionActive(Promotion promotion)
    {
        var currentDate = DateTime.Now;
        return promotion.StartDate <= currentDate && promotion.EndDate >= currentDate;
    }
}
}
