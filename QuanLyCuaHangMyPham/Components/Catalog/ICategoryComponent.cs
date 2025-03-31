// 1. Component (Interface)
using System.Collections.Generic;
using QuanLyCuaHangMyPham.Models;

namespace QuanLyCuaHangMyPham.Components.Categories
{
    public interface ICategoryComponent
    {
        int Id { get; }
        string Name { get; }
        int? ParentId { get; }
        Category Category { get; }
        void Display(int depth = 0);
        int CountCategories();
        int CountProducts();
        List<string> GetBreadcrumb();
    }
}