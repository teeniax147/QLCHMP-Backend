using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuanLyCuaHangMyPham.Data;
using QuanLyCuaHangMyPham.Models;
using QuanLyCuaHangMyPham.Services.Email;

namespace QuanLyCuaHangMyPham.Services.PROMOTIONS.Observer.Observers
{
    public class CustomerNotifier : IPromotionObserver
    {
        private readonly QuanLyCuaHangMyPhamContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<CustomerNotifier> _logger;

        public CustomerNotifier(
            QuanLyCuaHangMyPhamContext context,
            IEmailService emailService,
            ILogger<CustomerNotifier> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task OnPromotionCreated(Promotion promotion)
        {
            await SendPromotionNotifications(promotion, "mới");
        }

        public async Task OnPromotionUpdated(Promotion promotion)
        {
            // Thêm thông báo khi khuyến mãi được cập nhật
            await SendPromotionNotifications(promotion, "cập nhật");
        }

        public Task OnPromotionExpired(Promotion promotion)
        {
            // Không thông báo khi khuyến mãi hết hạn
            return Task.CompletedTask;
        }

        private async Task SendPromotionNotifications(Promotion promotion, string actionType)
        {
            if (!promotion.ProductId.HasValue)
                return;

            try
            {
                // Lấy thông tin sản phẩm
                var product = await _context.Products
                    .Include(p => p.Brand)
                    .FirstOrDefaultAsync(p => p.Id == promotion.ProductId.Value);

                if (product == null) return;

                // Tìm các khách hàng quan tâm (mở rộng từ chỉ khách hàng đã mua)
                var interestedCustomers = await GetInterestedCustomers(promotion.ProductId.Value);

                foreach (var customerId in interestedCustomers)
                {
                    try
                    {
                        var customer = await _context.Customers
                            .Include(c => c.User)
                            .FirstOrDefaultAsync(c => c.CustomerId == customerId);

                        if (customer?.User?.Email == null) continue;

                        // Gửi email HTML thay vì email văn bản thuần
                        string emailHtml = GeneratePromotionEmailHtml(
                            customer.User.FirstName ?? "Quý khách",
                            product,
                            promotion,
                            actionType
                        );

                        await _emailService.SendHtmlEmailAsync(
                            customer.User.Email,
                            $"Khuyến mãi {actionType} cho sản phẩm bạn quan tâm",
                            emailHtml
                        );

                        _logger.LogInformation($"Đã gửi email thông báo khuyến mãi cho khách hàng {customer.CustomerId}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Lỗi khi gửi email thông báo cho khách hàng {customerId}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý thông báo khuyến mãi qua email");
            }
        }

        // Mở rộng đối tượng khách hàng để bao gồm cả người đã thêm vào giỏ hàng/yêu thích
        private async Task<IEnumerable<int>> GetInterestedCustomers(int productId)
        {
            // Khách hàng đã mua sản phẩm
            var purchasedCustomers = await _context.OrderDetails
                .Where(od => od.ProductId == productId)
                .Select(od => od.Order.CustomerId)
                .Distinct()
                .ToListAsync();

            // Khách hàng đã thêm vào giỏ hàng nhưng chưa mua
            var cartCustomers = await _context.CartItems
                .Where(ci => ci.ProductId == productId)
                .Select(ci => ci.Cart.CustomerId)
                .Distinct()
                .ToListAsync();

            // Khách hàng đã thêm vào danh sách yêu thích
            var favoriteCustomers = await _context.Favorites
                .Where(f => f.ProductId == productId)
                .Select(f => f.UserId) // Giả sử Favorite có UserId thay vì CustomerId
                .Distinct()
                .ToListAsync();

            // Nếu cần ánh xạ UserId sang CustomerId
            List<int> favoriteCustomerIds = new List<int>();
            if (favoriteCustomers.Any())
            {
                favoriteCustomerIds = await _context.Customers
                    .Where(c => favoriteCustomers.Contains(c.UserId))
                    .Select(c => c.CustomerId)
                    .ToListAsync();
            }

            // Kết hợp tất cả các nhóm khách hàng và loại bỏ các ID trùng lặp
            return purchasedCustomers
                .Union(cartCustomers)
                .Union(favoriteCustomerIds)
                .Distinct();
        }

        // Tạo email HTML đẹp mắt thay vì email văn bản đơn giản
        private string GeneratePromotionEmailHtml(string customerName, Product product, Promotion promotion, string actionType)
        {
            // Tính ngày hết hạn khuyến mãi
            string expirationDate = promotion.EndDate.HasValue
                ? promotion.EndDate.Value.ToString("dd/MM/yyyy")
                : "không xác định";

            // Tính giá sau khuyến mãi
            decimal originalPrice = product.OriginalPrice;
            decimal discountAmount = originalPrice * (promotion.DiscountPercentage ?? 0) / 100;
            decimal discountedPrice = originalPrice - discountAmount;

            // Tạo HTML email đẹp mắt
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Khuyến mãi {actionType}</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #f8bbd0; padding: 20px; text-align: center; color: #880e4f; }}
        .content {{ padding: 20px; background-color: #fff; }}
        .footer {{ background-color: #f5f5f5; padding: 10px; text-align: center; font-size: 12px; color: #666; }}
        .product-info {{ border-left: 4px solid #f8bbd0; padding-left: 15px; margin: 15px 0; }}
        .product-name {{ font-size: 20px; font-weight: bold; color: #880e4f; margin-bottom: 10px; }}
        .price {{ font-size: 18px; margin: 10px 0; }}
        .original-price {{ text-decoration: line-through; color: #999; }}
        .discount-price {{ font-weight: bold; color: #d81b60; }}
        .button {{ display: inline-block; background-color: #d81b60; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px; margin-top: 15px; }}
        .promotion-details {{ background-color: #fce4ec; padding: 10px; border-radius: 4px; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Khuyến mãi {actionType} đặc biệt!</h1>
    </div>
    <div class='content'>
        <p>Xin chào {customerName},</p>
        <p>Chúng tôi vui mừng thông báo về chương trình khuyến mãi {actionType} cho sản phẩm mà bạn quan tâm:</p>
        
        <div class='product-info'>
            <div class='product-name'>{product.Name}</div>
            <p>{product.Description}</p>
            <div class='price'>
                <span class='original-price'>{originalPrice.ToString("#,##0")}đ</span> → 
                <span class='discount-price'>{discountedPrice.ToString("#,##0")}đ</span>
                <span> (Giảm {promotion.DiscountPercentage}%)</span>
            </div>
        </div>
        
        <div class='promotion-details'>
            <p><strong>Tên khuyến mãi:</strong> {promotion.Name}</p>
            <p><strong>Thời gian:</strong> {(promotion.StartDate?.ToString("dd/MM/yyyy") ?? "Ngay bây giờ")} đến {expirationDate}</p>
            <p><strong>Mức giảm giá:</strong> {promotion.DiscountPercentage}%</p>
        </div>
        
        <p>Đừng bỏ lỡ cơ hội tuyệt vời này để sở hữu sản phẩm bạn yêu thích với giá ưu đãi!</p>
        
        <a href='https://mycuahang.com/product/{product.Id}' class='button'>Xem sản phẩm ngay</a>
    </div>
    <div class='footer'>
        <p>Email này được gửi từ hệ thống thông báo tự động của cửa hàng mỹ phẩm. Vui lòng không trả lời email này.</p>
        <p>© 2024 Cửa hàng mỹ phẩm. Tất cả các quyền được bảo lưu.</p>
    </div>
</body>
</html>";
        }
    }
}