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
        using var ms = new MemoryStream();
        using var writer = new PdfWriter(ms);
        using var pdfDoc = new PdfDocument(writer);
        var document = new Document(pdfDoc);

        // Thêm tiêu đề
        document.Add(new Paragraph(title)
            .SetFontSize(20)
            .SetBold()
            .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));

        // Tạo bảng dữ liệu
        var table = new Table(data.First().GetType().GetProperties().Length);
        foreach (var prop in data.First().GetType().GetProperties())
        {
            table.AddHeaderCell(new Paragraph(prop.Name).SetBold());
        }

        foreach (var item in data)
        {
            foreach (var prop in item.GetType().GetProperties())
            {
                table.AddCell(new Paragraph(prop.GetValue(item)?.ToString() ?? ""));
            }
        }

        document.Add(table);
        document.Close();

        return ms.ToArray();
    }

    public byte[] ExportToExcel<T>(string title, List<T> data)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(title);

        // Tiêu đề cột
        var properties = data.First().GetType().GetProperties();
        for (int i = 0; i < properties.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = properties[i].Name;
        }

        // Dữ liệu
        for (int i = 0; i < data.Count; i++)
        {
            for (int j = 0; j < properties.Length; j++)
            {
                worksheet.Cells[i + 2, j + 1].Value = properties[j].GetValue(data[i]);
            }
        }

        return package.GetAsByteArray();
    }
}

