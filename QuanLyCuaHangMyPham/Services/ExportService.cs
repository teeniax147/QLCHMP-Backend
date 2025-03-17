using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using OfficeOpenXml;
using QuanLyCuaHangMyPham.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class ExportService
{
    public byte[] ExportToPdf<T>(string title, List<T> data)
    {
        if (data == null || !data.Any())
            throw new ArgumentException("Không có dữ liệu để xuất PDF.");

        using var ms = new MemoryStream();
        using var writer = new PdfWriter(ms);
        using var pdfDoc = new PdfDocument(writer);
        var document = new Document(pdfDoc);

        // Thêm tiêu đề
        document.Add(new Paragraph(title)
            .SetFontSize(20)
            .SetTextAlignment(TextAlignment.CENTER));

        // Tạo bảng dữ liệu
        var properties = data.First().GetType().GetProperties();
        var table = new Table(properties.Length);

        // Thêm tiêu đề cột
        foreach (var prop in properties)
        {
            table.AddHeaderCell(new Paragraph(prop.Name)
                .SetTextAlignment(TextAlignment.CENTER));
        }

        // Thêm dữ liệu vào bảng
        foreach (var item in data)
        {
            foreach (var prop in properties)
            {
                var value = prop.GetValue(item)?.ToString() ?? "Không có dữ liệu";
                table.AddCell(new Paragraph(value));
            }
        }

        document.Add(table);
        document.Close();

        return ms.ToArray();
    }

    public byte[] ExportToExcel<T>(string title, List<T> data)
    {
        if (data == null || !data.Any())
            throw new ArgumentException("Không có dữ liệu để xuất Excel.");

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(title);

        // Tiêu đề bảng
        worksheet.Cells["A1"].Value = title;
        worksheet.Cells["A1"].Style.Font.Size = 16;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1:H1"].Merge = true;
        worksheet.Cells["A1:H1"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

        // Tiêu đề cột bằng tiếng Việt
        var properties = data.First().GetType().GetProperties();
        worksheet.Cells[2, 1].Value = "Ngày"; // Thay "Date" thành "Ngày"
        worksheet.Cells[2, 2].Value = "Tổng doanh thu"; // Thay "TotalRevenue" thành "Tổng doanh thu"
        worksheet.Cells[2, 3].Value = "Tổng số đơn hàng"; // Thay "TotalOrders" thành "Tổng số đơn hàng"
        // Tiêu đề cột
        for (int i = 0; i < properties.Length; i++)
        {
            worksheet.Cells[2, i + 1].Style.Font.Bold = true;
            worksheet.Cells[2, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
        }
        // Thêm dữ liệu
        for (int i = 0; i < data.Count; i++)
        {
            for (int j = 0; j < properties.Length; j++)
            {
                worksheet.Cells[i + 3, j + 1].Value = properties[j].GetValue(data[i])?.ToString() ?? "N/A";
            }
        }

        // AutoFit cột
        worksheet.Cells.AutoFitColumns();

        return package.GetAsByteArray();
    }
}
