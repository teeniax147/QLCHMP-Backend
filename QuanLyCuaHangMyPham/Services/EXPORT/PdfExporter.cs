using System;
using System.Collections.Generic;
using System.IO;
using PdfSharp.Pdf;
using PdfSharp.Drawing;

namespace QuanLyCuaHangMyPham.Services.Export
{
    public class PdfExporter<T> : DocumentExporter<T>
    {
        protected override object CreateDocument()
        {
            return new PdfDocument();
        }

        protected override void AddTitle(object document, string title)
        {
            var doc = (PdfDocument)document;
            var page = doc.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            // Thiết lập vị trí vẽ
            double x = 40, y = 40;

            // Thêm tiêu đề
            gfx.DrawString(title, new XFont("Arial", 16), XBrushes.Black, x, y);
        }

        protected override void AddHeaders(object document, List<T> data)
        {
            var doc = (PdfDocument)document;
            var page = doc.Pages[0];
            var gfx = XGraphics.FromPdfPage(page);
            var font = new XFont("Arial", 12);

            // Thiết lập vị trí vẽ
            double x = 40, y = 70; // Dịch chuyển vị trí xuống dưới sau tiêu đề

            // Thêm tiêu đề cột
            var properties = typeof(T).GetProperties();
            foreach (var prop in properties)
            {
                gfx.DrawString(prop.Name, font, XBrushes.Black, x, y);
                x += 100; // Dịch chuyển cột
            }
        }

        protected override void AddData(object document, List<T> data)
        {
            var doc = (PdfDocument)document;
            var page = doc.Pages[0];
            var gfx = XGraphics.FromPdfPage(page);
            var font = new XFont("Arial", 12);

            double y = 90; // Dịch chuyển xuống dưới sau tiêu đề cột
            var properties = typeof(T).GetProperties();

            // Thêm dữ liệu vào bảng
            foreach (var item in data)
            {
                double x = 40; // Reset lại vị trí x
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(item)?.ToString() ?? "N/A";
                    gfx.DrawString(value, font, XBrushes.Black, x, y);
                    x += 100; // Dịch chuyển cột
                }
                y += 20; // Dịch chuyển xuống dưới sau mỗi dòng dữ liệu
            }
        }

        protected override byte[] SaveDocument(object document)
        {
            var doc = (PdfDocument)document;
            using (var ms = new MemoryStream())
            {
                doc.Save(ms);
                return ms.ToArray();
            }
        }
    }
}