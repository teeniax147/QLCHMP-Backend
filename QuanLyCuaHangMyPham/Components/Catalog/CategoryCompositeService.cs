using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyCuaHangMyPham.Components.Categories;
using QuanLyCuaHangMyPham.Data;
using QuanLyCuaHangMyPham.Models;

namespace QuanLyCuaHangMyPham.Services.Categories
{
    public class CategoryCompositeService
    {
        private readonly QuanLyCuaHangMyPhamContext _context;

        public CategoryCompositeService(QuanLyCuaHangMyPhamContext context)
        {
            _context = context;
        }

        // Xây dựng cây danh mục
        public async Task<List<CategoryComposite>> BuildCategoryTree(int? parentCategoryId = null)
        {
            var allCategories = await _context.Categories
                .Include(c => c.InverseParent)
                .Include(c => c.Products)
                .ToListAsync();

            var rootCategories = allCategories
                .Where(c => c.ParentId == parentCategoryId)
                .ToList();

            var result = new List<CategoryComposite>();

            foreach (var category in rootCategories)
            {
                var categoryComposite = new CategoryComposite(category);
                BuildCategoryTreeRecursive(categoryComposite, allCategories);
                result.Add(categoryComposite);
            }

            return result;
        }

        private void BuildCategoryTreeRecursive(CategoryComposite parentComposite, List<Category> allCategories)
        {
            var category = parentComposite.Category;

            var childCategories = allCategories
                .Where(c => c.ParentId == category.Id)
                .ToList();

            foreach (var childCategory in childCategories)
            {
                var childComposite = new CategoryComposite(childCategory);
                BuildCategoryTreeRecursive(childComposite, allCategories);
                parentComposite.Add(childComposite);
            }
        }

        // Lấy cấu trúc phân cấp dưới dạng DTO
        public async Task<List<CategoryHierarchyDto>> GetCategoryHierarchy(int? parentId = null)
        {
            var categories = await BuildCategoryTree(parentId);
            return categories.Select(ConvertToHierarchyDto).ToList();
        }

        // Lấy đường dẫn phân cấp của danh mục
        public async Task<List<BreadcrumbDto>> GetBreadcrumb(int categoryId)
        {
            var result = new List<BreadcrumbDto>();

            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null)
                return result;

            result.Add(new BreadcrumbDto
            {
                Id = category.Id,
                Name = category.Name
            });

            var parentId = category.ParentId;
            while (parentId.HasValue)
            {
                var parent = await _context.Categories.FindAsync(parentId.Value);
                if (parent == null)
                    break;

                result.Insert(0, new BreadcrumbDto
                {
                    Id = parent.Id,
                    Name = parent.Name
                });

                parentId = parent.ParentId;
            }

            return result;
        }

        // Lấy tất cả danh mục phẳng hoặc phân cấp
        public async Task<IEnumerable<Category>> GetAllCategories(bool hierarchical = false)
        {
            if (!hierarchical)
            {
                // Nếu không yêu cầu phân cấp, trả về danh sách phẳng
                return await _context.Categories
                    .Include(c => c.Parent)
                    .Include(c => c.InverseParent)
                    .ToListAsync();
            }
            else
            {
                // Nếu yêu cầu phân cấp, trả về dạng cây
                var categories = await _context.Categories
                    .Include(c => c.Parent)
                    .Include(c => c.InverseParent)
                    .ToListAsync();

                // Chuyển đổi sang dạng phân cấp
                var rootCategories = categories
                    .Where(c => c.ParentId == null)
                    .ToList();

                // Gán danh mục con cho từng danh mục
                foreach (var category in categories)
                {
                    category.InverseParent = categories
                        .Where(c => c.ParentId == category.Id)
                        .ToList();
                }

                return rootCategories;
            }
        }

        // Lấy danh mục theo ID kèm thông tin phân cấp
        public async Task<Category> GetCategoryById(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Parent)
                .Include(c => c.InverseParent)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return null;

            // Lấy tất cả các danh mục con (trực tiếp)
            category.InverseParent = await _context.Categories
                .Where(c => c.ParentId == id)
                .ToListAsync();

            return category;
        }

        // Lấy tất cả danh mục con (trực tiếp và gián tiếp) của một danh mục
        public async Task<IEnumerable<Category>> GetAllSubcategories(int parentId)
        {
            var allCategories = await _context.Categories.ToListAsync();
            var result = new List<Category>();

            // Lấy danh mục con trực tiếp
            var directChildren = allCategories
                .Where(c => c.ParentId == parentId)
                .ToList();

            result.AddRange(directChildren);

            // Lấy danh mục con gián tiếp bằng đệ quy
            foreach (var child in directChildren)
            {
                var subChildren = GetAllSubcategoriesRecursive(child.Id, allCategories);
                result.AddRange(subChildren);
            }

            return result;
        }

        private IEnumerable<Category> GetAllSubcategoriesRecursive(int parentId, List<Category> allCategories)
        {
            var children = allCategories
                .Where(c => c.ParentId == parentId)
                .ToList();

            var result = new List<Category>(children);

            foreach (var child in children)
            {
                var subChildren = GetAllSubcategoriesRecursive(child.Id, allCategories);
                result.AddRange(subChildren);
            }

            return result;
        }

        // Lấy danh mục có phân trang và tìm kiếm
        public async Task<(IEnumerable<Category> Categories, int TotalCount)> GetPagedCategories(
            int page, int pageSize, string search = null)
        {
            var query = _context.Categories.AsQueryable();

            // Tìm kiếm theo tên
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Name.Contains(search));
            }

            var totalCount = await query.CountAsync();

            var categories = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (categories, totalCount);
        }

        // Phương thức sắp xếp danh mục
        public async Task<IEnumerable<Category>> GetSortedCategories(string sortBy, bool ascending)
        {
            var query = _context.Categories.AsQueryable();

            // Sắp xếp theo cột được chọn
            query = sortBy.ToLower() switch
            {
                "name" => ascending ? query.OrderBy(c => c.Name) : query.OrderByDescending(c => c.Name),
                "createdat" => ascending ? query.OrderBy(c => c.CreatedAt) : query.OrderByDescending(c => c.CreatedAt),
                _ => query.OrderBy(c => c.Name) // Mặc định sắp xếp theo tên
            };

            return await query.ToListAsync();
        }

        // Kiểm tra danh mục tồn tại
        public async Task<bool> CategoryExists(int id)
        {
            return await _context.Categories.AnyAsync(c => c.Id == id);
        }

        // Chuyển CategoryComposite thành DTO
        private CategoryHierarchyDto ConvertToHierarchyDto(CategoryComposite category)
        {
            var dto = new CategoryHierarchyDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Category.Description,
                CreatedAt = category.Category.CreatedAt,
                ParentId = category.ParentId,
                ProductCount = category.CountProducts(),
                ChildCount = category.Children.Count,
                Children = new List<CategoryHierarchyDto>()
            };

            foreach (var child in category.Children.OfType<CategoryComposite>())
            {
                dto.Children.Add(ConvertToHierarchyDto(child));
            }

            return dto;
        }
    }

    // DTO classes
    public class CategoryHierarchyDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? ParentId { get; set; }
        public int ProductCount { get; set; }
        public int ChildCount { get; set; }
        public List<CategoryHierarchyDto> Children { get; set; } = new List<CategoryHierarchyDto>();
    }

    public class BreadcrumbDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}