using MimeKit;
using System;
using System.Collections.Generic;

namespace QuanLyCuaHangMyPham.Services.Email.Strategies
{
    public class PasswordResetConfirmationStrategy : IEmailBodyStrategy
    {
        private readonly Dictionary<string, object> _data;

        public PasswordResetConfirmationStrategy(Dictionary<string, object> data)
        {
            _data = data;
        }

        public MimeEntity CreateBody(string content)
        {
            // Lấy các thông tin từ data
            string userName = _data.ContainsKey("userName") ? _data["userName"].ToString() : "Quý khách";
            string resetTime = _data.ContainsKey("resetTime") ? _data["resetTime"].ToString() : DateTime.Now.ToString("HH:mm dd/MM/yyyy");
            string ipAddress = _data.ContainsKey("ipAddress") ? _data["ipAddress"].ToString() : "không xác định";

            // Tạo nội dung HTML đẹp mắt cho email xác nhận đặt lại mật khẩu
            string html = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px;'>
                <div style='text-align: center; margin-bottom: 20px;'>
                    <h2 style='color: #333;'>Mật Khẩu Đã Được Đặt Lại</h2>
                </div>
                
                <div style='padding: 20px; background-color: #f9f9f9; border-radius: 5px; margin-bottom: 20px;'>
                    <p style='margin-bottom: 15px;'>Xin chào <strong>{userName}</strong>,</p>
                    
                    <p style='margin-bottom: 15px;'>{content}</p>
                    
                    <div style='background-color: #f0f0f0; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                        <p style='margin: 0 0 10px 0;'><strong>Thời gian:</strong> {resetTime}</p>
                        <p style='margin: 0;'><strong>Địa chỉ IP:</strong> {ipAddress}</p>
                    </div>
                    
                    <p style='margin-bottom: 15px; color: #d9534f; font-weight: bold;'>
                        Nếu bạn không thực hiện hành động này, vui lòng liên hệ với chúng tôi ngay lập tức!
                    </p>
                    
                    <div style='margin: 25px 0; text-align: center;'>
                        <a href='#' style='display: inline-block; padding: 12px 24px; background-color: #337ab7; color: white; text-decoration: none; border-radius: 5px; font-weight: bold;'>
                            Liên Hệ Hỗ Trợ
                        </a>
                    </div>
                </div>
                
                <div style='background-color: #f5f5f5; padding: 15px; border-radius: 5px;'>
                    <p style='margin: 0; font-size: 12px; text-align: center; color: #777;'>
                        © {DateTime.Now.Year} Cửa Hàng Mỹ Phẩm. Tất cả các quyền được bảo lưu.<br>
                        Email này được gửi tự động, vui lòng không trả lời.
                    </p>
                </div>
            </div>";

            return new TextPart("html") { Text = html };
        }
    }
}