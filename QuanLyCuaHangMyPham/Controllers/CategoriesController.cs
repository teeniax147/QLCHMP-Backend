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

namespace QuanLyCuaHangMyPham.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;

        public CategoriesController(QuanLyCuaHangMyPhamContext context)
        {
            _context = context;
        }

        // GET: api/Categories - Lấy tất cả danh mục
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            return await _context.Categories
                .Include(c => c.Parent)
                .Include(c => c.InverseParent)
                .ToListAsync();
        }

        // GET: api/Categories/{id} - Lấy danh mục theo ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Parent)
                .Include(c => c.InverseParent)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound("Danh mục không tồn tại.");
            }

            return Ok(new { Message = "Lấy danh mục thành công", Category = category });
        }

        // POST: api/Categories - Tạo mới một danh mục
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Category>> CreateCategory(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, new { Message = "Tạo danh mục thành công", Category = category });
        }

        // PUT: api/Categories/{id} - Cập nhật danh mục theo ID
        
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
                if (!CategoryExists(id))
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

        // DELETE: api/Categories/{id} - Xóa danh mục theo ID
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

        // GET: api/Categories/parent/{parentId} - Lấy danh mục con theo ParentId
        [HttpGet("parent/{parentId}")]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategoriesByParentId(int parentId)
        {
            var categories = await _context.Categories
                .Where(c => c.ParentId == parentId)
                .ToListAsync();

            if (categories == null || categories.Count == 0)
            {
                return NotFound("Không có danh mục con nào.");
            }

            return Ok(categories);
        }
        // GET: api/Categories/paged - Lấy danh sách danh mục với phân trang và tìm kiếm
        [HttpGet("paged")]
        public async Task<ActionResult> GetPagedCategories([FromQuery] CategoryPagedRequest request)
        {
            var query = _context.Categories.AsQueryable();

            // Tìm kiếm theo tên
            if (!string.IsNullOrEmpty(request.Search))
            {
                query = query.Where(c => c.Name.Contains(request.Search));
            }

            var totalCategories = await query.CountAsync();
            var categories = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return Ok(new
            {
                TotalCategories = totalCategories,
                CurrentPage = request.Page,
                PageSize = request.PageSize,
                Categories = categories
            });
        }
        // GET: api/Categories/sorted - Sắp xếp danh mục
        [HttpGet("sorted")]
        public async Task<ActionResult<IEnumerable<Category>>> GetSortedCategories([FromQuery] CategorySortRequest request)
        {
            IQueryable<Category> query = _context.Categories;

            // Sắp xếp theo cột được chọn
            query = request.SortBy.ToLower() switch
            {
                "name" => request.Ascending ? query.OrderBy(c => c.Name) : query.OrderByDescending(c => c.Name),
                "createdat" => request.Ascending ? query.OrderBy(c => c.CreatedAt) : query.OrderByDescending(c => c.CreatedAt),
                _ => query.OrderBy(c => c.Name) // Mặc định sắp xếp theo tên
            };

            var categories = await query.ToListAsync();
            return Ok(categories);
        }
        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
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
