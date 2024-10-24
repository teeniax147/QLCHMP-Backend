using MailKit.Net.Smtp;
using MimeKit;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
namespace QuanLyCuaHangMyPham.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }


        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            _logger.LogInformation($"Bắt đầu gửi email tới {toEmail} với chủ đề {subject}");
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(_configuration["SmtpSettings:SenderName"], _configuration["SmtpSettings:SenderEmail"]));
            emailMessage.To.Add(new MailboxAddress(toEmail, toEmail));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart("plain")
            {
                Text = message
            };

            try
            {
                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(_configuration["SmtpSettings:Server"], int.Parse(_configuration["SmtpSettings:Port"]), MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(_configuration["SmtpSettings:Username"], _configuration["SmtpSettings:Password"]);
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

        // Phương thức tạo mã OTP
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

    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string message);
        string GenerateOTP(); // Thêm phương thức này vào interface
    }
}
