using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace QuanLyCuaHangMyPham.Services.Export
{
    public class ExcelExporter<T> : DocumentExporter<T>
    {
        protected override object CreateDocument()
        {
            return new XLWorkbook();
        }

        protected override void AddTitle(object document, string title)
        {
            var workbook = (XLWorkbook)document;
            var worksheet = workbook.Worksheets.Add(title);

            // Lấy số lượng cột dựa trên dữ liệu thực tế
            int columnCount = 4; // Cố định 4 cột cho báo cáo doanh thu

            // Thêm tiêu đề vào ô A1
            worksheet.Cell(1, 1).Value = title;

            // Merge và căn giữa tiêu đề
            worksheet.Range(1, 1, 1, columnCount).Merge()
                .Style.Font.SetBold()
                .Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        }

        protected override void AddHeaders(object document, List<T> data)
        {
            var workbook = (XLWorkbook)document;
            var worksheet = workbook.Worksheets.First();

            // Thêm tiêu đề cột tiếng Việt
            worksheet.Cell(2, 1).Value = "Ngày";
            worksheet.Cell(2, 2).Value = "Tổng doanh thu";
            worksheet.Cell(2, 3).Value = "Tổng số đơn hàng";
            worksheet.Cell(2, 4).Value = "Đơn hàng chi tiết";

            // Định dạng tiêu đề cột
            for (int i = 1; i <= 4; i++)
            {
                worksheet.Cell(2, i).Style.Font.SetBold()
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            }
        }

        protected override void AddData(object document, List<T> data)
        {
            var workbook = (XLWorkbook)document;
            var worksheet = workbook.Worksheets.First();

            // Xử lý dữ liệu phức tạp
            int row = 3; // Bắt đầu từ dòng 3 (dòng 1 là tiêu đề, dòng 2 là header)

            foreach (var item in data)
            {
                // Tạo một danh sách các cặp key-value từ thuộc tính của đối tượng
                var properties = item.GetType().GetProperties();

                // Đặt giá trị cho từng thuộc tính
                foreach (var prop in properties)
                {
                    string propName = prop.Name;
                    var value = prop.GetValue(item);

                    // Xử lý và điền giá trị vào cột tương ứng
                    if (propName == "Date" || propName.Contains("Date"))
                    {
                        if (value is DateTime dateValue)
                        {
                            worksheet.Cell(row, 1).Value = dateValue.ToString("dd/MM/yyyy");
                        }
                        else
                        {
                            worksheet.Cell(row, 1).Value = value?.ToString() ?? "N/A";
                        }
                    }
                    else if (propName == "TotalRevenue" || propName.Contains("Revenue"))
                    {
                        if (value is decimal decimalValue)
                        {
                            // Định dạng số tiền: không dấu phẩy và thêm "đ"
                            worksheet.Cell(row, 2).Value = $"{decimalValue:N0}đ";
                            // Căn phải cho số tiền
                            worksheet.Cell(row, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                        }
                        else
                        {
                            worksheet.Cell(row, 2).Value = value?.ToString() ?? "0đ";
                        }
                    }
                    else if (propName == "TotalOrders" || propName.Contains("Orders") && !(value is IEnumerable<object>))
                    {
                        // Vấn đề có thể ở đây - đổi thành string
                        worksheet.Cell(row, 3).Value = value?.ToString() ?? "0";
                        // Căn giữa cho số lượng đơn hàng
                        worksheet.Cell(row, 3).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    }
                    else if (propName == "Orders" && value is IEnumerable<object> orders)
                    {
                        int count = orders.Count();
                        worksheet.Cell(row, 4).Value = $"(Có {count} mục)";
                        // Căn giữa cho "Có X mục"
                        worksheet.Cell(row, 4).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    }
                }

                row++;
            }

            // Định dạng toàn bộ bảng
            var tableRange = worksheet.Range(2, 1, row - 1, 4);
            tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // AutoFit cột
            worksheet.Columns().AdjustToContents();
        }

        protected override byte[] SaveDocument(object document)
        {
            var workbook = (XLWorkbook)document;
            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
        }
    }
}