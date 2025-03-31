using MimeKit;

namespace QuanLyCuaHangMyPham.Services.Email.Strategies
{
    /// <summary>
    /// Chiến lược tạo email dạng văn bản thuần túy
    /// </summary>
    public class PlainTextBodyStrategy : IEmailBodyStrategy
    {
        public MimeEntity CreateBody(string content)
        {
            return new TextPart("plain")
            {
                Text = content
            };
        }
    }
}