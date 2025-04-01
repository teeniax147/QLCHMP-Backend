using MimeKit;
using QuanLyCuaHangMyPham.Services.Email.Strategies;

namespace QuanLyCuaHangMyPham.Services.Email.Decorators
{
    /// <summary>
    /// Decorator thêm branding vào email
    /// </summary>
    public class BrandingDecorator : EmailBodyDecorator
    {
        private readonly string _logoUrl;
        private readonly string _brandName;
        private readonly string _primaryColor;

        public BrandingDecorator(
            IEmailBodyStrategy component,
            string logoUrl = "https://mypham.com/logo.png",
            string brandName = "Cửa Hàng Mỹ Phẩm",
            string primaryColor = "#4CAF50")
            : base(component)
        {
            _logoUrl = logoUrl;
            _brandName = brandName;
            _primaryColor = primaryColor;
        }

        public override MimeEntity CreateBody(string content)
        {
            // Gọi component gốc
            var body = base.CreateBody(content);

            // Chỉ thêm branding vào email HTML
            if (body is TextPart textPart && textPart.ContentType.MimeType.Contains("html"))
            {
                string brandingHtml = $@"
                <div style='text-align: center; margin-bottom: 20px; padding: 15px; background-color: {_primaryColor}10;'>
                    <img src='{_logoUrl}' alt='{_brandName}' style='max-height: 60px; margin-bottom: 10px;' />
                    <h1 style='color: {_primaryColor}; margin: 0;'>{_brandName}</h1>
                </div>";

                if (textPart.Text.Contains("<body>"))
                {
                    textPart.Text = textPart.Text.Replace("<body>", $"<body>{brandingHtml}");
                }
                else if (textPart.Text.Contains("<body "))
                {
                    int bodyEndIndex = textPart.Text.IndexOf('>', textPart.Text.IndexOf("<body ")) + 1;
                    textPart.Text = textPart.Text.Insert(bodyEndIndex, brandingHtml);
                }
                else
                {
                    textPart.Text = brandingHtml + textPart.Text;
                }
            }

            return body;
        }
    }
}