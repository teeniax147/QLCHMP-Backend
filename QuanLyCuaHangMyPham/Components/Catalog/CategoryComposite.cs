using System;
using System.Collections.Generic;
using System.Linq;
using QuanLyCuaHangMyPham.Models;

namespace QuanLyCuaHangMyPham.Components.Categories
{
    public class CategoryComposite : ICategoryComponent
    {
        private readonly Category _category;
        private readonly List<ICategoryComponent> _children = new List<ICategoryComponent>();

        public CategoryComposite(Category category)
        {
            _category = category;
        }

        public int Id => _category.Id;

        public string Name => _category.Name;

        public int? ParentId => _category.ParentId;

        public Category Category => _category;

        public IReadOnlyList<ICategoryComponent> Children => _children.AsReadOnly();

        public void Add(ICategoryComponent component)
        {
            _children.Add(component);
        }

        public void Remove(ICategoryComponent component)
        {
            _children.Remove(component);
        }

        public void Display(int depth = 0)
        {
            Console.WriteLine($"{new string('-', depth)} {_category.Name}");

            foreach (var child in _children)
            {
                child.Display(depth + 2);
            }
        }

        public int CountCategories()
        {
            int count = 1; // Đếm danh mục hiện tại

            foreach (var child in _children)
            {
                count += child.CountCategories();
            }

            return count;
        }

        public int CountProducts()
        {
            int count = _category.Products.Count;

            foreach (var child in _children)
            {
                count += child.CountProducts();
            }

            return count;
        }

        public List<string> GetBreadcrumb()
        {
            return new List<string> { _category.Name };
        }

        public List<CategoryComposite> GetAllSubcategories()
        {
            var result = new List<CategoryComposite>();

            foreach (var child in _children)
            {
                if (child is CategoryComposite composite)
                {
                    result.Add(composite);
                    result.AddRange(composite.GetAllSubcategories());
                }
            }

            return result;
        }

        public CategoryComposite FindSubcategory(int id)
        {
            if (Id == id)
                return this;

            foreach (var child in _children)
            {
                if (child is CategoryComposite composite)
                {
                    var found = composite.FindSubcategory(id);
                    if (found != null)
                        return found;
                }
            }

            return null;
        }

        public bool IsParentOf(int childId)
        {
            foreach (var child in _children)
            {
                if (child.Id == childId)
                    return true;

                if (child is CategoryComposite composite && composite.IsParentOf(childId))
                    return true;
            }

            return false;
        }
    }
}