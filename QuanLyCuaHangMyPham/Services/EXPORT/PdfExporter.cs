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

            using (var gfx = XGraphics.FromPdfPage(page))
            {
                // Lấy kích thước trang
                double pageWidth = page.Width.Point;

                // Tạo font cho tiêu đề
                var titleFont = new XFont("Arial", 16);

                // Đo kích thước tiêu đề để căn giữa
                var titleSize = gfx.MeasureString(title, titleFont);

                // Tính toán vị trí x để căn giữa
                double x = (pageWidth - titleSize.Width) / 2;
                double y = 40;

                // Vẽ tiêu đề căn giữa
                gfx.DrawString(title, titleFont, XBrushes.Black, x, y);
            }
        }

        protected override void AddHeaders(object document, List<T> data)
        {
            var doc = (PdfDocument)document;
            if (doc.PageCount == 0)
                doc.AddPage();

            var page = doc.Pages[0];

            using (var gfx = XGraphics.FromPdfPage(page))
            {
                var headerFont = new XFont("Arial", 12);

                // Vị trí bắt đầu của header
                double startX = 40;
                double y = 70;

                // Vẽ các tiêu đề cột
                gfx.DrawString("Ngày", headerFont, XBrushes.Black, startX, y);
                gfx.DrawString("Tổng doanh thu", headerFont, XBrushes.Black, startX + 100, y);
                gfx.DrawString("Tổng số đơn hàng", headerFont, XBrushes.Black, startX + 250, y);
                gfx.DrawString("Đơn hàng chi tiết", headerFont, XBrushes.Black, startX + 400, y);

                // Vẽ đường kẻ dưới header
                gfx.DrawLine(new XPen(XColors.Black, 1), 40, y + 5, 500, y + 5);
            }
        }

        protected override void AddData(object document, List<T> data)
        {
            var doc = (PdfDocument)document;
            if (doc.PageCount == 0)
                doc.AddPage();

            var page = doc.Pages[0];

            using (var gfx = XGraphics.FromPdfPage(page))
            {
                var font = new XFont("Arial", 12);

                // Thiết lập vị trí vẽ
                double startX = 40;
                double y = 90;

                // Vẽ dữ liệu
                foreach (var item in data)
                {
                    var properties = item.GetType().GetProperties();

                    DateTime? date = null;
                    decimal? revenue = null;
                    int? totalOrders = null;
                    IEnumerable<object> orders = null;

                    // Lấy giá trị của các thuộc tính
                    foreach (var prop in properties)
                    {
                        string propName = prop.Name;
                        var value = prop.GetValue(item);

                        if (propName == "Date" || propName.Contains("Date"))
                        {
                            if (value is DateTime dateValue)
                                date = dateValue;
                        }
                        else if (propName == "TotalRevenue" || propName.Contains("Revenue"))
                        {
                            if (value is decimal decimalValue)
                                revenue = decimalValue;
                        }
                        else if (propName == "TotalOrders" || propName.Contains("Orders") && !(value is IEnumerable<object>))
                        {
                            if (value is int intValue)
                                totalOrders = intValue;
                        }
                        else if (propName == "Orders" && value is IEnumerable<object> ordersValue)
                        {
                            orders = ordersValue;
                        }
                    }

                    // Vẽ dữ liệu từng cột
                    if (date.HasValue)
                        gfx.DrawString(date.Value.ToString("dd/MM/yyyy"), font, XBrushes.Black, startX, y);

                    if (revenue.HasValue)
                    {
                        string formattedRevenue = $"{revenue.Value:N0}đ";
                        // Đo kích thước để căn phải
                        var textSize = gfx.MeasureString(formattedRevenue, font);
                        gfx.DrawString(formattedRevenue, font, XBrushes.Black, startX + 200 - textSize.Width, y);
                    }

                    if (totalOrders.HasValue)
                    {
                        string ordersText = totalOrders.Value.ToString();
                        // Đo kích thước để căn giữa
                        var textSize = gfx.MeasureString(ordersText, font);
                        gfx.DrawString(ordersText, font, XBrushes.Black, startX + 250 + 50 - textSize.Width / 2, y);
                    }

                    if (orders != null)
                    {
                        int count = orders.Count();
                        string countText = $"(Có {count} mục)";
                        gfx.DrawString(countText, font, XBrushes.Black, startX + 400, y);
                    }

                    y += 20;

                    // Kiểm tra xem có cần tạo trang mới không
                    if (y > page.Height - 50)
                    {
                        page = doc.AddPage();
                        y = 40;

                        // Vẽ lại tiêu đề cột trên trang mới
                        using (var newPageGfx = XGraphics.FromPdfPage(page))
                        {
                            var headerFont = new XFont("Arial", 12);

                            newPageGfx.DrawString("Ngày", headerFont, XBrushes.Black, startX, 20);
                            newPageGfx.DrawString("Tổng doanh thu", headerFont, XBrushes.Black, startX + 100, 20);
                            newPageGfx.DrawString("Tổng số đơn hàng", headerFont, XBrushes.Black, startX + 250, 20);
                            newPageGfx.DrawString("Đơn hàng chi tiết", headerFont, XBrushes.Black, startX + 400, 20);

                            newPageGfx.DrawLine(new XPen(XColors.Black, 1), 40, 25, 500, 25);
                        }
                    }
                }
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