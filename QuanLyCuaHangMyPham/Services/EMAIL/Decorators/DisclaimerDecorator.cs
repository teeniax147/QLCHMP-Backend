using MimeKit;
using QuanLyCuaHangMyPham.Services.Email.Strategies;

namespace QuanLyCuaHangMyPham.Services.Email.Decorators
{
    /// <summary>
    /// Decorator thêm disclaimer vào email
    /// </summary>
    public class DisclaimerDecorator : EmailBodyDecorator
    {
        private readonly string _disclaimerText;

        public DisclaimerDecorator(
            IEmailBodyStrategy component,
            string disclaimerText = null)
            : base(component)
        {
            _disclaimerText = disclaimerText ??
                "Email này chỉ dành cho người nhận được đề cập trong email. " +
                "Nếu bạn nhận được email này do nhầm lẫn, vui lòng xóa và thông báo cho người gửi.";
        }

        public override MimeEntity CreateBody(string content)
        {
            // Gọi component gốc
            var body = base.CreateBody(content);

            if (body is TextPart textPart)
            {
                string mimeType = textPart.ContentType.MimeType;

                if (mimeType.Contains("html"))
                {
                    string disclaimerHtml = $@"
                    <div style='margin-top: 20px; border-top: 1px dotted #ccc; padding-top: 10px; font-size: 11px; color: #999; font-style: italic;'>
                        {_disclaimerText}
                    </div>";

                    if (textPart.Text.Contains("</body>"))
                    {
                        textPart.Text = textPart.Text.Replace("</body>", $"{disclaimerHtml}</body>");
                    }
                    else
                    {
                        textPart.Text += disclaimerHtml;
                    }
                }
                else
                {
                    string disclaimerText = $@"
---------------------------
DISCLAIMER: {_disclaimerText}";

                    textPart.Text += disclaimerText;
                }
            }

            return body;
        }
    }
}