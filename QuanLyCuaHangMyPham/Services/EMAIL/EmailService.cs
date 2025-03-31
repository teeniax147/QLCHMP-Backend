using MailKit.Net.Smtp;
using MimeKit;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using QuanLyCuaHangMyPham.Services.Email.Strategies;

namespace QuanLyCuaHangMyPham.Services.Email
{
    /// <summary>
    /// Dịch vụ gửi email, đóng vai trò là Context trong Strategy Pattern
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        // Các chiến lược mặc định
        private readonly IEmailBodyStrategy _plainTextStrategy;
        private readonly IEmailBodyStrategy _htmlStrategy;

        /// <summary>
        /// Khởi tạo dịch vụ email
        /// </summary>
        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Khởi tạo các chiến lược
            _plainTextStrategy = new PlainTextBodyStrategy();
            _htmlStrategy = new HtmlBodyStrategy();
        }

        /// <summary>
        /// Gửi email với chiến lược cụ thể - phương thức chính của Strategy Pattern
        /// </summary>
        public async Task SendEmailWithStrategyAsync(string toEmail, string subject, string content, IEmailBodyStrategy strategy)
        {
            if (strategy == null)
                throw new ArgumentNullException(nameof(strategy), "Email body strategy không được null");

            // Sử dụng chiến lược để tạo nội dung
            var body = strategy.CreateBody(content);

            // Gửi email với nội dung đã tạo
            await SendEmailMessageAsync(toEmail, subject, body);
        }

        /// <summary>
        /// Phương thức cơ sở để gửi email - không thay đổi khi thêm chiến lược mới
        /// </summary>
        private async Task SendEmailMessageAsync(string toEmail, string subject, MimeEntity body)
        {
            _logger.LogInformation($"Bắt đầu gửi email tới {toEmail} với chủ đề {subject}");
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(
                _configuration["SmtpSettings:SenderName"],
                _configuration["SmtpSettings:SenderEmail"]));
            emailMessage.To.Add(new MailboxAddress(toEmail, toEmail));
            emailMessage.Subject = subject;
            emailMessage.Body = body;

            try
            {
                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(
                        _configuration["SmtpSettings:Server"],
                        int.Parse(_configuration["SmtpSettings:Port"]),
                        MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(
                        _configuration["SmtpSettings:Username"],
                        _configuration["SmtpSettings:Password"]);
                    await client.SendAsync(emailMessage);
                    await client.DisconnectAsync(true);
                }
                _logger.LogInformation($"Email đã gửi thành công tới {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi gửi email tới {toEmail}: {ex.Message}");
                throw new InvalidOperationException("Lỗi khi gửi email", ex);
            }
        }

        /// <summary>
        /// Gửi email văn bản thuần túy
        /// </summary>
        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            await SendEmailWithStrategyAsync(toEmail, subject, message, _plainTextStrategy);
        }

        /// <summary>
        /// Gửi email HTML
        /// </summary>
        public async Task SendHtmlEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            await SendEmailWithStrategyAsync(toEmail, subject, htmlMessage, _htmlStrategy);
        }

        /// <summary>
        /// Gửi email với file đính kèm
        /// </summary>
        public async Task SendEmailWithAttachmentAsync(string toEmail, string subject, string message, string attachmentPath)
        {
            var attachmentStrategy = new MultipartBodyStrategy(attachmentPath);
            await SendEmailWithStrategyAsync(toEmail, subject, message, attachmentStrategy);
        }

        /// <summary>
        /// Tạo mã OTP ngẫu nhiên để xác thực
        /// </summary>
        public string GenerateOTP()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                var tokenData = new byte[4]; // Độ dài 4 byte
                rng.GetBytes(tokenData);
                int otpValue = BitConverter.ToUInt16(tokenData, 0) % 1000000; // Lấy số từ 0 đến 999999
                return otpValue.ToString("D6"); // Đảm bảo có 6 chữ số
            }
        }
    }
}