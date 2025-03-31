using MimeKit;
using System.Collections.Generic;

namespace QuanLyCuaHangMyPham.Services.Email.Strategies
{
    /// <summary>
    /// Chiến lược tạo email sử dụng template engine
    /// </summary>
    public class TemplatedEmailStrategy : IEmailBodyStrategy
    {
        private readonly string _templateName;
        private readonly IDictionary<string, object> _templateData;
        private readonly ITemplateEngine _templateEngine;
        private readonly bool _isHtml;
        
        /// <summary>
        /// Khởi tạo chiến lược email dựa trên template
        /// </summary>
        /// <param name="templateName">Tên template cần sử dụng</param>
        /// <param name="templateData">Dữ liệu để điền vào template</param>
        /// <param name="templateEngine">Template engine sử dụng để render</param>
        /// <param name="isHtml">Có phải template HTML không?</param>
        public TemplatedEmailStrategy(
            string templateName,
            IDictionary<string, object> templateData,
            ITemplateEngine templateEngine,
            bool isHtml = true)
        {
            _templateName = templateName;
            _templateData = templateData;
            _templateEngine = templateEngine;
            _isHtml = isHtml;
        }
        
        public MimeEntity CreateBody(string content)
        {
            // Kết hợp nội dung truyền vào với template data
            if (!_templateData.ContainsKey("content"))
            {
                _templateData.Add("content", content);
            }
            
            // Render template
            string renderedContent = _templateEngine.Render(_templateName, _templateData);
            
            // Tạo phần nội dung email tương ứng
            return new TextPart(_isHtml ? "html" : "plain")
            {
                Text = renderedContent
            };
        }
    }
    
    /// <summary>
    /// Interface cho template engine, cần được triển khai cụ thể tùy theo engine bạn chọn
    /// </summary>
    public interface ITemplateEngine
    {
        string Render(string templateName, IDictionary<string, object> data);
    }
}