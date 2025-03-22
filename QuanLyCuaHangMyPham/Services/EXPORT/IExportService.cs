using System.Collections.Generic;

namespace QuanLyCuaHangMyPham.Services.Export
{
    public interface IExportService
    {
        byte[] ExportToExcel<T>(string title, List<T> data);
        byte[] ExportToPdf<T>(string title, List<T> data);
    }
}