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

            // Tiêu đề bảng
            worksheet.Cell(1, 1).Value = title;
            worksheet.Range(1, 1, 1, typeof(T).GetProperties().Length).Merge()
                .Style.Font.SetBold().Font.SetFontSize(16).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        }

        protected override void AddHeaders(object document, List<T> data)
        {
            var workbook = (XLWorkbook)document;
            var worksheet = workbook.Worksheets.First();
            var properties = typeof(T).GetProperties();

            // Tiêu đề cột
            for (int i = 0; i < properties.Length; i++)
            {
                worksheet.Cell(2, i + 1).Style.Font.SetBold().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                worksheet.Cell(2, i + 1).Value = properties[i].Name;
            }
        }

        protected override void AddData(object document, List<T> data)
        {
            var workbook = (XLWorkbook)document;
            var worksheet = workbook.Worksheets.First();
            var properties = typeof(T).GetProperties();

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