using MimeKit;
using System.IO;

namespace QuanLyCuaHangMyPham.Services.Email.Strategies
{
    /// <summary>
    /// Chiến lược tạo email có đính kèm file
    /// </summary>
    public class MultipartBodyStrategy : IEmailBodyStrategy
    {
        private readonly string _attachmentPath;
        private readonly string _contentType;

        /// <summary>
        /// Khởi tạo chiến lược email có file đính kèm
        /// </summary>
        /// <param name="attachmentPath">Đường dẫn đến file đính kèm</param>
        /// <param name="contentType">Loại nội dung, mặc định là application/octet-stream</param>
        public MultipartBodyStrategy(string attachmentPath, string contentType = "application/octet-stream")
        {
            _attachmentPath = attachmentPath;
            _contentType = contentType;
        }

        public MimeEntity CreateBody(string content)
        {
            // Tạo email dạng multipart để có thể chứa cả text và attachment
            var multipart = new Multipart("mixed");

            // Thêm phần text
            multipart.Add(new TextPart("plain")
            {
                Text = content
            });

            // Tách loại nội dung thành media type và subtype
            string[] contentTypeParts = _contentType.Split('/');
            string mediaType = contentTypeParts[0];
            string subType = contentTypeParts.Length > 1 ? contentTypeParts[1] : "octet-stream";

            // Thêm attachment
            var attachment = new MimePart(mediaType, subType)
            {
                Content = new MimeContent(File.OpenRead(_attachmentPath)),
                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                ContentTransferEncoding = ContentEncoding.Base64,
                FileName = Path.GetFileName(_attachmentPath)
            };

            multipart.Add(attachment);

            return multipart;
        }
    }
}