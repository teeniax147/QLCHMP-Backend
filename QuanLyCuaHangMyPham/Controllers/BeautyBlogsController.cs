using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using QuanLyCuaHangMyPham.Services;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using QuanLyCuaHangMyPham.Data;
using Microsoft.EntityFrameworkCore;
using QuanLyCuaHangMyPham.Models;
using QuanLyCuaHangMyPham.IdentityModels;
using Microsoft.AspNetCore.Authorization;

namespace QuanLyCuaHangMyPham.Controllers
{
    [Route("api/beauty-blog")]
    [ApiController]
    public class BeautyBlogController : ControllerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;

        public BeautyBlogController(QuanLyCuaHangMyPhamContext context)
        {
            _context = context;
        }

        // GET: api/beauty-blog - Lấy tất cả các bài viết
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BeautyBlog>>> GetAllBlogs()
        {
            var blogs = await _context.BeautyBlogs.Include(b => b.Category).ToListAsync();
            return Ok(new { Message = "Lấy danh sách bài viết thành công", Blogs = blogs });
        }

        // GET: api/beauty-blog/{id} - Lấy bài viết theo ID
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetBlogById(int id)
        {
            var blog = await _context.BeautyBlogs.Include(b => b.Category).FirstOrDefaultAsync(b => b.Id == id);

            if (blog == null)
            {
                return NotFound(new { Message = "Bài viết không tồn tại." });
            }

            return Ok(new { Message = "Lấy bài viết thành công", Blog = blog });
        }

        // POST: api/beauty-blog/them-moi - Tạo một bài viết mới
        [Authorize(Roles = "Admin")]
        [HttpPost("them-moi")]
        public async Task<ActionResult<BeautyBlog>> CreateBlog(BeautyBlogCreateRequest request)
        {
            var blog = new BeautyBlog
            {
                Title = request.Title,
                Content = request.Content,
                Author = request.Author,
                FeaturedImage = request.FeaturedImage,
                CategoryId = request.CategoryId,
                ScheduledPublishDate = request.ScheduledPublishDate,
                CreatedAt = DateTime.UtcNow,
                ViewCount = 0
            };

            _context.BeautyBlogs.Add(blog);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBlogById), new { id = blog.Id }, new { Message = "Tạo bài viết mới thành công", Blog = blog });
        }

        // PUT: api/beauty-blog/cap-nhat/{id} - Cập nhật bài viết
        [Authorize(Roles = "Admin")]
        [HttpPut("cap-nhat/{id}")]
        public async Task<IActionResult> UpdateBlog(int id, BeautyBlogUpdateRequest request)
        {
            if (id != request.Id)
            {
                return BadRequest("ID không khớp.");
            }

            var blog = await _context.BeautyBlogs.FindAsync(id);
            if (blog == null)
            {
                return NotFound("Bài viết không tồn tại.");
            }

            blog.Title = request.Title;
            blog.Content = request.Content;
            blog.Author = request.Author;
            blog.FeaturedImage = request.FeaturedImage;
            blog.CategoryId = request.CategoryId;
            blog.UpdatedAt = DateTime.UtcNow;

            _context.Entry(blog).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok("Cập nhật bài viết thành công.");
        }


        // DELETE: api/beauty-blog/xoa/{id} - Xóa bài viết
        [Authorize(Roles = "Admin")]
        [HttpDelete("xoa/{id}")]
        public async Task<IActionResult> DeleteBlog(int id)
        {
            var blog = await _context.BeautyBlogs.FindAsync(id);
            if (blog == null)
            {
                return NotFound("Bài viết không tồn tại.");
            }

            _context.BeautyBlogs.Remove(blog);
            await _context.SaveChangesAsync();

            return Ok("Xóa bài viết thành công.");
        }

        // GET: api/beauty-blog/paged - Lấy danh sách bài viết với phân trang và tìm kiếm
        [HttpGet("danh-sach-phan-trang")]
        public async Task<ActionResult<IEnumerable<BeautyBlog>>> GetPagedBlogs(int page = 1, int pageSize = 10, string? search = null)
        {
            var query = _context.BeautyBlogs.Include(b => b.Category).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(b => b.Title.Contains(search) || b.Content.Contains(search));
            }

            var totalBlogs = await query.CountAsync();
            var blogs = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                Message = "Lấy danh sách bài viết với phân trang thành công",
                TotalBlogs = totalBlogs,
                CurrentPage = page,
                PageSize = pageSize,
                Blogs = blogs
            });
        }

        // GET: api/beauty-blog/category/{categoryId} - Lấy bài viết theo danh mục
        [HttpGet("danh-muc/{categoryId}")]
        public async Task<ActionResult<IEnumerable<BeautyBlog>>> GetBlogsByCategory(int categoryId)
        {
            var blogs = await _context.BeautyBlogs
                .Where(b => b.CategoryId == categoryId)
                .Include(b => b.Category)
                .ToListAsync();

            if (!blogs.Any())
            {
                return NotFound("Không có bài viết nào trong danh mục này.");
            }

            return Ok(new { Message = "Lấy danh sách bài viết theo danh mục thành công", Blogs = blogs });
        }

        // GET: api/beauty-blog/sap-xep - Sắp xếp bài viết
        [HttpGet("sap-xep")]
        public async Task<ActionResult<IEnumerable<BeautyBlog>>> GetSortedBlogs(string sortBy = "createdAt", bool ascending = false)
        {
            IQueryable<BeautyBlog> query = _context.BeautyBlogs.Include(b => b.Category);

            query = sortBy.ToLower() switch
            {
                "createdat" => ascending ? query.OrderBy(b => b.CreatedAt) : query.OrderByDescending(b => b.CreatedAt),
                "updatedat" => ascending ? query.OrderBy(b => b.UpdatedAt) : query.OrderByDescending(b => b.UpdatedAt),
                _ => query.OrderByDescending(b => b.CreatedAt)
            };

            var blogs = await query.ToListAsync();
            return Ok(new { Message = "Sắp xếp bài viết thành công", Blogs = blogs });
        }

        // GET: api/beauty-blog/{id}/tang-luot-xem - Tăng số lượt xem của bài viết
        [HttpGet("{id}/tang-luot-xem")]
        public async Task<IActionResult> IncrementViewCount(int id)
        {
            var blog = await _context.BeautyBlogs.FindAsync(id);

            if (blog == null)
            {
                return NotFound("Bài viết không tồn tại.");
            }

            blog.ViewCount += 1;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Tăng lượt xem thành công", ViewCount = blog.ViewCount });
        }

        // POST: api/beauty-blog/len-lich - Lên lịch đăng bài
        [Authorize(Roles = "Admin")]
        [HttpPost("len-lich")]
        public async Task<IActionResult> ScheduleBlogPost([FromBody] BeautyBlogScheduleRequest request)
        {
            // Kiểm tra xem CategoryId có hợp lệ không
            if (!await _context.Categories.AnyAsync(c => c.Id == request.CategoryId))
            {
                return BadRequest("Danh mục không tồn tại.");
            }
            var blog = new BeautyBlog
            {
                Title = request.Title,
                Content = request.Content,
                Author = request.Author,
                FeaturedImage = request.FeaturedImage,
                CategoryId = request.CategoryId,
                ScheduledPublishDate = request.ScheduledPublishDate,
                CreatedAt = null
            };

            _context.BeautyBlogs.Add(blog);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBlogById), new { id = blog.Id }, new { Message = "Lên lịch bài viết thành công", Blog = blog });
        }

        // GET: api/beauty-blog/dang-bai-viet-len-lich - Đăng các bài viết đã được lên lịch
        [Authorize(Roles = "Admin")]
        [HttpGet("dang-bai-viet-len-lich")]
        public async Task<IActionResult> PublishScheduledBlogs()
        {
            var now = DateTime.UtcNow;
            var scheduledBlogs = await _context.BeautyBlogs
                .Where(b => b.CreatedAt == null && b.ScheduledPublishDate <= now)
                .ToListAsync();

            foreach (var blog in scheduledBlogs)
            {
                blog.CreatedAt = now;
            }

            await _context.SaveChangesAsync();

            return Ok("Đã đăng các bài viết lên lịch thành công.");
        }

        private bool BlogExists(int id)
        {
            return _context.BeautyBlogs.Any(b => b.Id == id);
        }
        // Lớp yêu cầu để tạo một bài viết mới
        public class BeautyBlogCreateRequest
        {
            [Required(ErrorMessage = "Tiêu đề là bắt buộc.")]
            public string Title { get; set; } = null!;

            [Required(ErrorMessage = "Nội dung là bắt buộc.")]
            public string Content { get; set; } = null!;

            public string? Author { get; set; }

            public string? FeaturedImage { get; set; }

            public int? CategoryId { get; set; }

            public DateTime? ScheduledPublishDate { get; set; }
        }

        // Lớp yêu cầu để cập nhật bài viết
        public class BeautyBlogUpdateRequest
        {
            [Required(ErrorMessage = "ID là bắt buộc.")]
            public int Id { get; set; }

            [Required(ErrorMessage = "Tiêu đề là bắt buộc.")]
            public string Title { get; set; } = null!;

            [Required(ErrorMessage = "Nội dung là bắt buộc.")]
            public string Content { get; set; } = null!;

            public string? Author { get; set; }

            public string? FeaturedImage { get; set; }

            public int? CategoryId { get; set; }
        }

        // Lớp yêu cầu để lên lịch một bài viết
        public class BeautyBlogScheduleRequest
        {
            [Required(ErrorMessage = "Tiêu đề là bắt buộc.")]
            public string Title { get; set; } = null!;

            [Required(ErrorMessage = "Nội dung là bắt buộc.")]
            public string Content { get; set; } = null!;

            public string? Author { get; set; }

            public string? FeaturedImage { get; set; }

            public int? CategoryId { get; set; }

            [Required(ErrorMessage = "Ngày lên lịch là bắt buộc.")]
            public DateTime ScheduledPublishDate { get; set; }
        }
    }
}