using MimeKit;
using QuanLyCuaHangMyPham.Services.Email.Strategies;
using System;

namespace QuanLyCuaHangMyPham.Services.Email.Decorators
{
    /// <summary>
    /// Decorator thêm tracking pixel vào email
    /// </summary>
    public class TrackingPixelDecorator : EmailBodyDecorator
    {
        private readonly string _trackingUrl;
        private readonly string _emailId;

        public TrackingPixelDecorator(
            IEmailBodyStrategy component,
            string trackingUrl = "https://mypham.com/pixel.gif",
            string emailId = null)
            : base(component)
        {
            _trackingUrl = trackingUrl;
            _emailId = emailId ?? Guid.NewGuid().ToString();
        }

        public override MimeEntity CreateBody(string content)
        {
            // Gọi component gốc
            var body = base.CreateBody(content);

            // Chỉ thêm tracking pixel vào email HTML
            if (body is TextPart textPart && textPart.ContentType.MimeType.Contains("html"))
            {
                // Tạo tracking pixel
                string trackingPixel = $@"<img src=""{_trackingUrl}?id={_emailId}"" width=""1"" height=""1"" alt="""" style=""display:none;"" />";

                if (textPart.Text.Contains("</body>"))
                {
                    textPart.Text = textPart.Text.Replace("</body>", $"{trackingPixel}</body>");
                }
                else
                {
                    textPart.Text += trackingPixel;
                }
            }

            return body;
        }
    }
}