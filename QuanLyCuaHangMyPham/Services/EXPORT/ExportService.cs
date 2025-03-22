using System.Collections.Generic;

namespace QuanLyCuaHangMyPham.Services.Export
{
    public class ExportService : IExportService
    {
        public byte[] ExportToExcel<T>(string title, List<T> data)
        {
            var exporter = new ExcelExporter<T>();
            return exporter.Export(title, data);
        }

        public byte[] ExportToPdf<T>(string title, List<T> data)
        {
            var exporter = new PdfExporter<T>();
            return exporter.Export(title, data);
        }
    }
}