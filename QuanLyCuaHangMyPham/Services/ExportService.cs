using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using PdfSharp.Pdf;
using PdfSharp.Drawing;

public class ExportService
{
    public byte[] ExportToExcel<T>(string title, List<T> data)
    {
        if (data == null || !data.Any())
            throw new ArgumentException("Không có dữ liệu để xuất Excel.");

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(title);

        // Tiêu đề bảng
        worksheet.Cell(1, 1).Value = title;
        worksheet.Range(1, 1, 1, data.First().GetType().GetProperties().Length).Merge()
            .Style.Font.SetBold().Font.SetFontSize(16).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        // Tiêu đề cột (bằng tiếng Việt)
        worksheet.Cell(2, 1).Value = "Ngày"; // Thay "Date" thành "Ngày"
        worksheet.Cell(2, 2).Value = "Tổng doanh thu"; // Thay "TotalRevenue" thành "Tổng doanh thu"
        worksheet.Cell(2, 3).Value = "Tổng số đơn hàng"; // Thay "TotalOrders" thành "Tổng số đơn hàng"

        // Tiêu đề cột (dựa trên các thuộc tính của dữ liệu)
        var properties = data.First().GetType().GetProperties();
        for (int i = 0; i < properties.Length; i++)
        {
            worksheet.Cell(2, i + 1).Style.Font.SetBold().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            worksheet.Cell(2, i + 1).Value = properties[i].Name;
        }

        // Thêm dữ liệu
        for (int i = 0; i < data.Count; i++)
        {
            for (int j = 0; j < properties.Length; j++)
            {
                worksheet.Cell(i + 3, j + 1).Value = properties[j].GetValue(data[i])?.ToString() ?? "N/A";
            }
        }

        // AutoFit cột
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportToPdf<T>(string title, List<T> data)
    {
        if (data == null || !data.Any())
            throw new ArgumentException("Không có dữ liệu để xuất PDF.");

        using var ms = new MemoryStream();
        var doc = new PdfDocument();
        var page = doc.AddPage();
        var gfx = XGraphics.FromPdfPage(page);
        var font = new XFont("Arial", 12);

        // Thiết lập vị trí vẽ
        double x = 40, y = 40;

        // Thêm tiêu đề
        gfx.DrawString(title, new XFont("Arial", 16), XBrushes.Black, x, y);
        y += 30; // Dịch chuyển vị trí xuống dưới sau tiêu đề

        // Thêm tiêu đề cột
        var properties = data.First().GetType().GetProperties();
        foreach (var prop in properties)
        {
            gfx.DrawString(prop.Name, font, XBrushes.Black, x, y);
            x += 100; // Dịch chuyển cột
        }

        y += 20; // Dịch chuyển xuống dưới sau tiêu đề cột

        // Thêm dữ liệu vào bảng
        foreach (var item in data)
        {
            x = 40; // Reset lại vị trí x
            foreach (var prop in properties)
            {
                var value = prop.GetValue(item)?.ToString() ?? "N/A";
                gfx.DrawString(value, font, XBrushes.Black, x, y);
                x += 100; // Dịch chuyển cột
            }
            y += 20; // Dịch chuyển xuống dưới sau mỗi dòng dữ liệu
        }

        // Lưu file PDF vào bộ nhớ
        doc.Save(ms);
        return ms.ToArray();
    }
}
