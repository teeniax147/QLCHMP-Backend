using QuanLyCuaHangMyPham.Services.Email.Decorators;
using QuanLyCuaHangMyPham.Services.Email.Strategies;
using System;

namespace QuanLyCuaHangMyPham.Services.Email.Extensions
{
    /// <summary>
    /// Extension methods cho IEmailBodyStrategy để dễ dàng sử dụng các decorator
    /// </summary>
    public static class EmailStrategyExtensions
    {
        /// <summary>
        /// Thêm footer vào email
        /// </summary>
        public static IEmailBodyStrategy WithFooter(this IEmailBodyStrategy component,
            string companyName = "Cửa Hàng Mỹ Phẩm",
            string contactInfo = "Email: contact@mypham.com | SĐT: 0123.456.789",
            string address = "123 Đường ABC, Quận XYZ, TP.HCM")
        {
            return new FooterDecorator(component, companyName, contactInfo, address);
        }

        /// <summary>
        /// Thêm pixel tracking vào email
        /// </summary>
        public static IEmailBodyStrategy WithTracking(this IEmailBodyStrategy component,
            string trackingUrl = "https://mypham.com/pixel.gif",
            string emailId = null)
        {
            return new TrackingPixelDecorator(component, trackingUrl, emailId ?? Guid.NewGuid().ToString());
        }

        /// <summary>
        /// Thêm branding vào email
        /// </summary>
        public static IEmailBodyStrategy WithBranding(this IEmailBodyStrategy component,
            string logoUrl = "https://mypham.com/logo.png",
            string brandName = "Cửa Hàng Mỹ Phẩm",
            string primaryColor = "#4CAF50")
        {
            return new BrandingDecorator(component, logoUrl, brandName, primaryColor);
        }

        /// <summary>
        /// Thêm disclaimer vào email
        /// </summary>
        public static IEmailBodyStrategy WithDisclaimer(this IEmailBodyStrategy component, string disclaimerText = null)
        {
            return new DisclaimerDecorator(component, disclaimerText);
        }
    }
}