using MimeKit;

namespace QuanLyCuaHangMyPham.Services.Email.Strategies
{
    /// <summary>
    /// Interface định nghĩa chiến lược tạo nội dung email.
    /// Mỗi chiến lược sẽ tạo một loại nội dung email khác nhau.
    /// </summary>
    public interface IEmailBodyStrategy
    {
        /// <summary>
        /// Tạo nội dung email từ chuỗi nội dung đầu vào
        /// </summary>
        /// <param name="content">Nội dung cần chuyển đổi</param>
        /// <returns>MimeEntity chứa nội dung đã được định dạng</returns>
        MimeEntity CreateBody(string content);
    }
}