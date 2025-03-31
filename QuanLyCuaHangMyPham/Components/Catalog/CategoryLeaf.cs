// 2. Leaf (Danh mục đơn giản không có danh mục con)
using System;
using System.Collections.Generic;
using System.Linq;
using QuanLyCuaHangMyPham.Models;

namespace QuanLyCuaHangMyPham.Components.Categories
{
    public class CategoryLeaf : ICategoryComponent
    {
        private readonly Category _category;

        public CategoryLeaf(Category category)
        {
            _category = category;
        }

        public int Id => _category.Id;
        public string Name => _category.Name;
        public int? ParentId => _category.ParentId;
        public Category Category => _category;

        public void Display(int depth = 0)
        {
            Console.WriteLine($"{new string('-', depth)} {_category.Name}");
        }

        public int CountCategories()
        {
            return 1; // Chỉ đếm danh mục hiện tại
        }

        public int CountProducts()
        {
            return _category.Products?.Count ?? 0; // Chỉ đếm sản phẩm trực tiếp
        }

        public List<string> GetBreadcrumb()
        {
            return new List<string> { _category.Name };
        }
    }
}
