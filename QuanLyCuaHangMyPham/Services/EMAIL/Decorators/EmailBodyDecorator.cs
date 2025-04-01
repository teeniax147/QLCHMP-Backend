using MimeKit;
using QuanLyCuaHangMyPham.Services.Email.Strategies;
using System;

namespace QuanLyCuaHangMyPham.Services.Email.Decorators
{
    /// <summary>
    /// Lớp decorator cơ sở cho các email strategy theo mẫu thiết kế Decorator Pattern.
    /// </summary>
    public abstract class EmailBodyDecorator : IEmailBodyStrategy
    {
        // Tham chiếu đến component được trang trí
        protected readonly IEmailBodyStrategy _component;

        /// <summary>
        /// Khởi tạo decorator với component cần trang trí
        /// </summary>
        /// <param name="component">Component cần trang trí</param>
        public EmailBodyDecorator(IEmailBodyStrategy component)
        {
            _component = component ?? throw new ArgumentNullException(nameof(component));
        }

        /// <summary>
        /// Chuyển tiếp thao tác tạo email đến component
        /// </summary>
        public virtual MimeEntity CreateBody(string content)
        {
            return _component.CreateBody(content);
        }
    }
}