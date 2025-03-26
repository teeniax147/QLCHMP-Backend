using System.Collections.Generic;

namespace QuanLyCuaHangMyPham.Components.Categories
{
    public interface ICategoryComponent
    {
        int Id { get; }
        string Name { get; }
        int? ParentId { get; }

        void Display(int depth = 0);
        int CountCategories();
        int CountProducts();
        List<string> GetBreadcrumb();
    }
}