using System.Threading.Tasks;
using QuanLyCuaHangMyPham.Services.Email.Strategies;

namespace QuanLyCuaHangMyPham.Services.Email
{
    /// <summary>
    /// Interface định nghĩa dịch vụ gửi email
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Gửi email sử dụng chiến lược cụ thể
        /// </summary>
        Task SendEmailWithStrategyAsync(string toEmail, string subject, string content, IEmailBodyStrategy strategy);

        /// <summary>
        /// Gửi email văn bản thuần túy
        /// </summary>
        Task SendEmailAsync(string toEmail, string subject, string message);

        /// <summary>
        /// Gửi email HTML
        /// </summary>
        Task SendHtmlEmailAsync(string toEmail, string subject, string htmlMessage);

        /// <summary>
        /// Gửi email có đính kèm file
        /// </summary>
        Task SendEmailWithAttachmentAsync(string toEmail, string subject, string message, string attachmentPath);

        /// <summary>
        /// Tạo mã OTP để xác thực
        /// </summary>
        string GenerateOTP();
    }
}