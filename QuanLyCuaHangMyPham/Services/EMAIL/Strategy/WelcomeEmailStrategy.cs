using MimeKit;
using System;
using System.Collections.Generic;

namespace QuanLyCuaHangMyPham.Services.Email.Strategies
{
    public class WelcomeEmailStrategy : IEmailBodyStrategy
    {
        private readonly Dictionary<string, object> _data;

        public WelcomeEmailStrategy(Dictionary<string, object> data)
        {
            _data = data;
        }

        public MimeEntity CreateBody(string content)
        {
            // Lấy các thông tin từ data
            string userName = _data.ContainsKey("userName") ? _data["userName"].ToString() : "Quý khách";
            string shopName = _data.ContainsKey("shopName") ? _data["shopName"].ToString() : "Cửa Hàng Mỹ Phẩm";
            string registrationDate = _data.ContainsKey("registrationDate") ? _data["registrationDate"].ToString() : DateTime.Now.ToString("dd/MM/yyyy");

            // Tạo nội dung HTML đẹp mắt cho email chào mừng
            string html = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px;'>
                <div style='text-align: center; margin-bottom: 20px;'>
                    <h2 style='color: #333;'>Chào Mừng Đến Với {shopName}!</h2>
                </div>
                
                <div style='padding: 20px; background-color: #f9f9f9; border-radius: 5px; margin-bottom: 20px;'>
                    <p style='margin-bottom: 15px;'>Xin chào <strong>{userName}</strong>,</p>
                    
                    <p style='margin-bottom: 15px;'>{content}</p>
                    
                    <div style='margin: 25px 0; text-align: center;'>
                        <a href='#' style='display: inline-block; padding: 12px 24px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 5px; font-weight: bold;'>
                            Khám Phá Ngay
                        </a>
                    </div>
                    
                    <p style='margin-bottom: 10px;'>Thông tin tài khoản của bạn:</p>
                    <ul style='background-color: #f0f0f0; padding: 15px; border-radius: 5px;'>
                        <li style='margin-bottom: 5px;'>Ngày đăng ký: <strong>{registrationDate}</strong></li>
                        <li style='margin-bottom: 5px;'>Cấp độ thành viên: <strong>Thành viên mới</strong></li>
                    </ul>
                </div>
                
                <div style='background-color: #f5f5f5; padding: 15px; border-radius: 5px;'>
                    <p style='margin: 0; font-size: 12px; text-align: center; color: #777;'>
                        © {DateTime.Now.Year} {shopName}. Tất cả các quyền được bảo lưu.<br>
                        Địa chỉ: 123 Đường ABC, Quận XYZ, TP.HCM<br>
                        Điện thoại: 0123.456.789 | Email: support@mypham.com
                    </p>
                </div>
            </div>";

            return new TextPart("html") { Text = html };
        }
    }
}