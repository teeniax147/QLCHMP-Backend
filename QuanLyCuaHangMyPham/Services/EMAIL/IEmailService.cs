using System.Threading.Tasks;

namespace QuanLyCuaHangMyPham.Services.Email
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string message);
        Task SendHtmlEmailAsync(string toEmail, string subject, string htmlMessage);
        string GenerateOTP();
    }
}