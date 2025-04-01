using MimeKit;
using QuanLyCuaHangMyPham.Services.Email.Strategies;
using System;

namespace QuanLyCuaHangMyPham.Services.Email.Decorators
{
    /// <summary>
    /// Decorator thêm footer vào email
    /// </summary>
    public class FooterDecorator : EmailBodyDecorator
    {
        private readonly string _companyName;
        private readonly string _contactInfo;
        private readonly string _address;
        private readonly string _year;

        public FooterDecorator(
            IEmailBodyStrategy component,
            string companyName = "Cửa Hàng Mỹ Phẩm",
            string contactInfo = "Email: contact@mypham.com | SĐT: 0123.456.789",
            string address = "123 Đường ABC, Quận XYZ, TP.HCM")
            : base(component)
        {
            _companyName = companyName;
            _contactInfo = contactInfo;
            _address = address;
            _year = DateTime.Now.Year.ToString();
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
                    // Footer HTML
                    string footerHtml = $@"
                    <div style='margin-top: 20px; border-top: 1px solid #eee; padding-top: 10px; font-size: 12px; color: #777;'>
                        <p>&copy; {_year} {_companyName}. Tất cả các quyền được bảo lưu.</p>
                        <p>{_contactInfo}</p>
                        <p>{_address}</p>
                    </div>";

                    if (textPart.Text.Contains("</body>"))
                    {
                        textPart.Text = textPart.Text.Replace("</body>", $"{footerHtml}</body>");
                    }
                    else
                    {
                        textPart.Text += footerHtml;
                    }
                }
                else
                {
                    // Footer text
                    string footerText = $@"
---------------------------
© {_year} {_companyName}. Tất cả các quyền được bảo lưu.
{_contactInfo}
{_address}";

                    textPart.Text += footerText;
                }
            }

            return body;
        }
    }
}