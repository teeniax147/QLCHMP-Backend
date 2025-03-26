using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyCuaHangMyPham.Data;
using QuanLyCuaHangMyPham.Models;
using QuanLyCuaHangMyPham.Services.Categories;

namespace QuanLyCuaHangMyPham.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;
        private readonly CategoryCompositeService _categoryService;

        public CategoriesController(
            QuanLyCuaHangMyPhamContext context,
            CategoryCompositeService categoryService)
        {
            _context = context;
            _categoryService = categoryService;
        }

        // GET: api/Categories - Lấy tất cả danh mục (tinh chỉnh)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories([FromQuery] bool hierarchical = false)
        {
            var categories = await _categoryService.GetAllCategories(hierarchical);
            return Ok(categories);
        }

        // GET: api/Categories/{id} - Lấy danh mục theo ID (tinh chỉnh)
        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            var category = await _categoryService.GetCategoryById(id);

            if (category == null)
            {
                return NotFound("Danh mục không tồn tại.");
            }

            return Ok(new { Message = "Lấy danh mục thành công", Category = category });
        }

        // POST: api/Categories - Tạo mới một danh mục (giữ nguyên)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Category>> CreateCategory(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, new { Message = "Tạo danh mục thành công", Category = category });
        }

        // PUT: api/Categories/{id} - Cập nhật danh mục theo ID (tinh chỉnh)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCategory(int id, Category category)
        {
            if (id != category.Id)
            {
                return BadRequest("ID không khớp.");
            }

            _context.Entry(category).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _categoryService.CategoryExists(id))
                {
                    return NotFound("Danh mục không tồn tại.");
                }
                else
                {
                    throw;
                }
            }

            return Ok("Cập nhật danh mục thành công.");
        }

        // DELETE: api/Categories/{id} - Xóa danh mục theo ID (giữ nguyên)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound("Danh mục không tồn tại.");
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok("Xóa danh mục thành công.");
        }

        // GET: api/Categories/parent/{parentId} - Lấy danh mục con theo ParentId (tinh chỉnh)
        [HttpGet("parent/{parentId}")]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategoriesByParentId(int parentId, [FromQuery] bool recursive = false)
        {
            if (!recursive)
            {
                // Lấy chỉ danh mục con trực tiếp (giữ nguyên hành vi cũ)
                var categories = await _context.Categories
                    .Where(c => c.ParentId == parentId)
                    .ToListAsync();

                if (categories == null || categories.Count == 0)
                {
                    return NotFound("Không có danh mục con nào.");
                }

                return Ok(categories);
            }
            else
            {
                // Lấy tất cả danh mục con (trực tiếp và gián tiếp)
                var allSubcategories = await _categoryService.GetAllSubcategories(parentId);

                if (!allSubcategories.Any())
                {
                    return NotFound("Không có danh mục con nào.");
                }

                return Ok(allSubcategories);
            }
        }

        // GET: api/Categories/paged - Lấy danh sách danh mục với phân trang và tìm kiếm (tinh chỉnh)
        [HttpGet("paged")]
        public async Task<ActionResult> GetPagedCategories([FromQuery] CategoryPagedRequest request)
        {
            var (categories, totalCategories) = await _categoryService.GetPagedCategories(
                request.Page, request.PageSize, request.Search);

            return Ok(new
            {
                TotalCategories = totalCategories,
                CurrentPage = request.Page,
                PageSize = request.PageSize,
                Categories = categories
            });
        }

        // GET: api/Categories/sorted - Sắp xếp danh mục (tinh chỉnh)
        [HttpGet("sorted")]
        public async Task<ActionResult<IEnumerable<Category>>> GetSortedCategories([FromQuery] CategorySortRequest request)
        {
            var categories = await _categoryService.GetSortedCategories(request.SortBy, request.Ascending);
            return Ok(categories);
        }

        // Thêm endpoints mới
        // GET: api/Categories/hierarchy - Lấy cấu trúc phân cấp danh mục
        [HttpGet("hierarchy")]
        public async Task<IActionResult> GetCategoryHierarchy(int? parentId = null)
        {
            var hierarchy = await _categoryService.GetCategoryHierarchy(parentId);
            return Ok(new
            {
                Message = "Lấy cấu trúc phân cấp danh mục thành công",
                Categories = hierarchy
            });
        }

        // GET: api/Categories/{id}/breadcrumb - Lấy đường dẫn phân cấp
        [HttpGet("{id}/breadcrumb")]
        public async Task<IActionResult> GetCategoryBreadcrumb(int id)
        {
            var breadcrumb = await _categoryService.GetBreadcrumb(id);
            if (!breadcrumb.Any())
            {
                return NotFound("Danh mục không tồn tại");
            }

            return Ok(new
            {
                Message = "Lấy đường dẫn phân cấp thành công",
                Breadcrumb = breadcrumb
            });
        }

        public class CategoryPagedRequest
        {
            public int Page { get; set; } = 1;
            public int PageSize { get; set; } = 10;
            public string? Search { get; set; }
        }

        public class CategorySortRequest
        {
            public string SortBy { get; set; } = "createdAt"; // Các giá trị có thể là "name", "createdAt"
            public bool Ascending { get; set; } = true;
        }
    }
}