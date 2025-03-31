using MimeKit;
using System;
using System.Collections.Generic;

namespace QuanLyCuaHangMyPham.Services.Email.Strategies
{
    public class OtpEmailStrategy : IEmailBodyStrategy
    {
        private readonly string _otp;
        private readonly string _purpose;
        private readonly string _expireTime;

        // Constructor hiện tại - giữ nguyên để tương thích ngược
        public OtpEmailStrategy(string otp, string purpose)
        {
            _otp = otp;
            _purpose = purpose;
            _expireTime = "5 phút";
        }

        // Constructor mới nhận Dictionary
        public OtpEmailStrategy(Dictionary<string, object> data)
        {
            _otp = data.ContainsKey("otp") ? data["otp"].ToString() : "000000";
            _purpose = data.ContainsKey("purpose") ? data["purpose"].ToString() : "xác thực";
            _expireTime = data.ContainsKey("expireTime") ? data["expireTime"].ToString() : "5 phút";
        }

        public MimeEntity CreateBody(string content)
        {
            // Code tạo nội dung email
            string html = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px;'>
                <div style='text-align: center; margin-bottom: 20px;'>
                    <h2 style='color: #333;'>Mã Xác Thực OTP</h2>
                </div>
                
                <div style='padding: 20px; background-color: #f9f9f9; border-radius: 5px; margin-bottom: 20px;'>
                    <p style='margin-bottom: 15px;'>{content}</p>
                    
                    <div style='text-align: center; margin: 25px 0;'>
                        <div style='display: inline-block; padding: 15px 25px; background-color: #f5f5f5; border-radius: 5px; border: 1px dashed #ccc;'>
                            <span style='font-size: 24px; font-weight: bold; letter-spacing: 5px; color: #333;'>{_otp}</span>
                        </div>
                    </div>
                    
                    <p style='margin-bottom: 10px;'>Mã này dùng để <strong>{_purpose}</strong> và sẽ hết hạn sau <strong>{_expireTime}</strong>.</p>
                    <p style='font-size: 13px; color: #777;'>Nếu bạn không yêu cầu mã này, bạn có thể bỏ qua email này.</p>
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