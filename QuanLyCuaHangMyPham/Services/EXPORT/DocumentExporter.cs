using System;
using System.Collections.Generic;
using System.Linq;

namespace QuanLyCuaHangMyPham.Services.Export
{
    // Lớp trừu tượng định nghĩa template cho việc xuất document
    public abstract class DocumentExporter<T>
    {
        // Template Method định nghĩa thuật toán chung
        public byte[] Export(string title, List<T> data)
        {
            ValidateData(data);
            var document = CreateDocument();
            AddTitle(document, title);
            AddHeaders(document, data);
            AddData(document, data);
            return SaveDocument(document);
        }

        // Phương thức chung có thể sử dụng ở tất cả các lớp con
        protected virtual void ValidateData(List<T> data)
        {
            if (data == null || !data.Any())
                throw new ArgumentException("Không có dữ liệu để xuất.");
        }

        // Các phương thức trừu tượng phải được triển khai bởi các lớp con
        protected abstract object CreateDocument();
        protected abstract void AddTitle(object document, string title);
        protected abstract void AddHeaders(object document, List<T> data);
        protected abstract void AddData(object document, List<T> data);
        protected abstract byte[] SaveDocument(object document);
    }
}