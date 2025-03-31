using MimeKit;

namespace QuanLyCuaHangMyPham.Services.Email.Strategies
{
    /// <summary>
    /// Chiến lược tạo email dạng HTML
    /// </summary>
    public class HtmlBodyStrategy : IEmailBodyStrategy
    {
        public MimeEntity CreateBody(string content)
        {
            return new TextPart("html")
            {
                Text = content
            };
        }
    }
}