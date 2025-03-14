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
        public async Task<ActionResult<BeautyBlog>> CreateBlog([FromForm] BeautyBlogCreateRequest request)
        {
            try
            {
                string? imagePath = null;

                if (request.FeaturedImage != null && request.FeaturedImage.Length > 0)
                {
                    var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(request.FeaturedImage.FileName);
                    var filePath = Path.Combine(uploadDir, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await request.FeaturedImage.CopyToAsync(stream);
                    }

                    imagePath = $"/uploads/{uniqueFileName}";
                }

                var blog = new BeautyBlog
                {
                    Title = request.Title,
                    Content = request.Content,
                    Author = request.Author,
                    FeaturedImage = imagePath,
                    CategoryId = request.CategoryId,
                    ScheduledPublishDate = request.ScheduledPublishDate,
                    CreatedAt = DateTime.UtcNow,
                    ViewCount = 0
                };

                _context.BeautyBlogs.Add(blog);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetBlogById), new { id = blog.Id }, new { Message = "Tạo bài viết mới thành công", Blog = blog });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Đã xảy ra lỗi khi xử lý file tải lên.", Error = ex.Message });
            }
        }

        // PUT: api/beauty-blog/cap-nhat/{id} - Cập nhật bài viết
        [Authorize(Roles = "Admin")]
        [HttpPut("cap-nhat/{id}")]
        public async Task<IActionResult> UpdateBlog(int id, [FromForm] BeautyBlogUpdateRequest request)
        {        

            var blog = await _context.BeautyBlogs.FindAsync(id);
            if (blog == null)
            {
                return NotFound("Bài viết không tồn tại.");
            }

            string? imagePath = blog.FeaturedImage; // Giữ ảnh cũ nếu không cập nhật ảnh mới

            if (request.FeaturedImage != null && request.FeaturedImage.Length > 0)
            {
                // Xử lý lưu file ảnh mới
                var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(request.FeaturedImage.FileName);
                var filePath = Path.Combine(uploadDir, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await request.FeaturedImage.CopyToAsync(stream);
                }

                // Gán đường dẫn file mới
                imagePath = $"/uploads/{uniqueFileName}";

                // Xóa ảnh cũ nếu có (tránh để file rác)
                if (!string.IsNullOrEmpty(blog.FeaturedImage))
                {
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", blog.FeaturedImage.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }
            }

            // Cập nhật các thông tin khác
            blog.Title = request.Title;
            blog.Content = request.Content;
            blog.Author = request.Author;
            blog.FeaturedImage = imagePath;
            blog.CategoryId = request.CategoryId;
            blog.UpdatedAt = DateTime.UtcNow;

            _context.Entry(blog).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Cập nhật bài viết thành công", Blog = blog });
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

        // GET: api/beauty-blog/danh-sach-phan-trang - Lấy danh sách bài viết với phân trang và tìm kiếm
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

        // GET: api/beauty-blog/danh-muc/{categoryId} - Lấy bài viết theo danh mục
        [HttpGet("danh-muc/{categoryId}")]
        public async Task<ActionResult<IEnumerable<BeautyBlog>>> GetBlogsByCategory(int categoryId)
        {
            var blogs = await _context.BeautyBlogs
                .Where(b => b.CategoryId == categoryId)
                .Include(b => b.Category)
                .ToListAsync();

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
        public async Task<IActionResult> ScheduleBlogPost([FromForm] BeautyBlogScheduleRequest request)
        {
            // Kiểm tra danh mục tồn tại
            if (!await _context.Categories.AnyAsync(c => c.Id == request.CategoryId))
            {
                return BadRequest("Danh mục không tồn tại.");
            }

            string? imagePath = null;

            // Xử lý file ảnh tải lên
            if (request.FeaturedImage != null && request.FeaturedImage.Length > 0)
            {
                var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(request.FeaturedImage.FileName);
                var filePath = Path.Combine(uploadDir, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await request.FeaturedImage.CopyToAsync(stream);
                }

                imagePath = $"/uploads/{uniqueFileName}";
            }

            // Tạo bài viết mới
            var blog = new BeautyBlog
            {
                Title = request.Title,
                Content = request.Content,
                Author = request.Author,
                FeaturedImage = imagePath,
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

        public class BeautyBlogCreateRequest
        {
            [Required(ErrorMessage = "Tiêu đề là bắt buộc.")]
            public string Title { get; set; } = null!;

            [Required(ErrorMessage = "Nội dung là bắt buộc.")]
            public string Content { get; set; } = null!;

            public string? Author { get; set; }

            public IFormFile? FeaturedImage { get; set; } // Đây phải là IFormFile

            public int? CategoryId { get; set; }

            public DateTime? ScheduledPublishDate { get; set; }
        }

        public class BeautyBlogUpdateRequest
        {           

            [Required(ErrorMessage = "Tiêu đề là bắt buộc.")]
            public string Title { get; set; } = null!;

            [Required(ErrorMessage = "Nội dung là bắt buộc.")]
            public string Content { get; set; } = null!;

            public string? Author { get; set; }

            public IFormFile? FeaturedImage { get; set; } // Đây phải là IFormFile

            public int? CategoryId { get; set; }
        }

        public class BeautyBlogScheduleRequest
        {
            [Required(ErrorMessage = "Tiêu đề là bắt buộc.")]
            public string Title { get; set; } = null!;

            [Required(ErrorMessage = "Nội dung là bắt buộc.")]
            public string Content { get; set; } = null!;

            public string? Author { get; set; }

            public IFormFile? FeaturedImage { get; set; } // Hỗ trợ upload file ảnh

            public int? CategoryId { get; set; }

            [Required(ErrorMessage = "Ngày lên lịch là bắt buộc.")]
            public DateTime ScheduledPublishDate { get; set; }
        }

    }
}
