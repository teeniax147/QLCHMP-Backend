using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyCuaHangMyPham.Data;
using QuanLyCuaHangMyPham.Models;

namespace QuanLyCuaHangMyPham.Controllers
{
    [Route("api/thuong-hieu")]
    [ApiController]
    public class BrandController : ControllerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;

        public BrandController(QuanLyCuaHangMyPhamContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Brand>>> GetAllBrands()
        {
            var brands = await _context.Brands.Include(b => b.Products).ToListAsync();

            foreach (var brand in brands)
            {
                if (!string.IsNullOrEmpty(brand.LogoUrl))
                {
                    brand.LogoUrl = $"{Request.Scheme}://{Request.Host}{brand.LogoUrl}";
                }
            }

            return Ok(brands);
        }

        // GET: api/thuong-hieu/{id} - Lấy thương hiệu theo ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Brand>> GetBrandById(int id)
        {
            var brand = await _context.Brands.Include(b => b.Products).FirstOrDefaultAsync(b => b.Id == id);

            if (brand == null)
            {
                return NotFound("Không tìm thấy thương hiệu.");
            }

            return Ok(brand);
        }

        // POST: api/thuong-hieu/them-moi - Tạo một thương hiệu mới (chỉ Admin)
        [Authorize(Roles = "Admin")]
        [HttpPost("them-moi")]
        public async Task<IActionResult> CreateBrand([FromForm] BrandCreateDTO brandDto)
        {
            try
            {
                string logoPath = null;

                // Xử lý upload logo
                if (brandDto.LogoFile != null && brandDto.LogoFile.Length > 0)
                {
                    var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "brands");
                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(brandDto.LogoFile.FileName);
                    var filePath = Path.Combine(uploadDir, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await brandDto.LogoFile.CopyToAsync(stream);
                    }

                    logoPath = $"/uploads/brands/{uniqueFileName}";
                }

                var brand = new Brand
                {
                    Name = brandDto.Name,
                    Description = brandDto.Description,
                    LogoUrl = logoPath,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Brands.Add(brand);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetBrandById), new { id = brand.Id }, new
                {
                    message = "Thương hiệu mới đã được tạo thành công.",
                    data = brand
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi tạo thương hiệu.", error = ex.Message });
            }
        }

        // PUT: api/thuong-hieu/cap-nhat/{id} - Cập nhật thương hiệu (chỉ Admin)
        [Authorize(Roles = "Admin")]
        [HttpPut("cap-nhat/{id}")]
        public async Task<IActionResult> UpdateBrand(int id, [FromForm] BrandUpdateDTO brandDto)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
            {
                return NotFound(new { message = "Thương hiệu không tồn tại." });
            }

            string logoPath = brand.LogoUrl; // Giữ logo cũ nếu không có logo mới

            // Xử lý logo mới nếu có
            if (brandDto.LogoFile != null && brandDto.LogoFile.Length > 0)
            {
                var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "brands");
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(brandDto.LogoFile.FileName);
                var filePath = Path.Combine(uploadDir, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await brandDto.LogoFile.CopyToAsync(stream);
                }

                // Xóa logo cũ nếu có
                if (!string.IsNullOrEmpty(brand.LogoUrl))
                {
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", brand.LogoUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                logoPath = $"/uploads/brands/{uniqueFileName}";
            }

            // Cập nhật thông tin thương hiệu
            brand.Name = brandDto.Name;
            brand.Description = brandDto.Description;
            brand.LogoUrl = logoPath;


            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Cập nhật thương hiệu thành công.", data = brand });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BrandExists(id))
                {
                    return NotFound(new { message = "Thương hiệu không tồn tại." });
                }
                else
                {
                    throw;
                }
            }
        }

        // DELETE: api/thuong-hieu/xoa/{id} - Xóa thương hiệu (chỉ Admin)
        [Authorize(Roles = "Admin")]
        [HttpDelete("xoa/{id}")]
        public async Task<IActionResult> DeleteBrand(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
            {
                return NotFound(new { message = "Thương hiệu không tồn tại." });
            }

            // Xóa logo nếu có
            if (!string.IsNullOrEmpty(brand.LogoUrl))
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", brand.LogoUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.Brands.Remove(brand);

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Xóa thương hiệu thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi xóa thương hiệu.", error = ex.Message });
            }
        }

        // GET: api/thuong-hieu/danh-sach-phan-trang - Lấy danh sách thương hiệu với phân trang và tìm kiếm
        [HttpGet("danh-sach-phan-trang")]
        public async Task<ActionResult<BrandPagedListDTO>> GetPagedBrands(int page = 1, int pageSize = 10, string? search = null)
        {
            var query = _context.Brands.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(b => b.Name.Contains(search));
            }

            var totalBrands = await query.CountAsync();
            var brands = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BrandSummaryDTO
                {
                    Id = b.Id,
                    Name = b.Name,
                    LogoUrl = b.LogoUrl
                })
                .ToListAsync();

            var result = new BrandPagedListDTO
            {
                TotalBrands = totalBrands,
                CurrentPage = page,
                PageSize = pageSize,
                Brands = brands
            };

            return Ok(result);
        }

        // GET: api/thuong-hieu/loc - Lọc thương hiệu theo tên hoặc ngày tạo
        [HttpGet("loc")]
        public async Task<ActionResult<IEnumerable<Brand>>> FilterBrands(string? name = null, DateTime? createdFrom = null, DateTime? createdTo = null)
        {
            var query = _context.Brands.AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(b => b.Name.Contains(name));
            }

            if (createdFrom.HasValue)
            {
                query = query.Where(b => b.CreatedAt >= createdFrom.Value);
            }

            if (createdTo.HasValue)
            {
                query = query.Where(b => b.CreatedAt <= createdTo.Value);
            }

            return await query.ToListAsync();
        }

        // GET: api/thuong-hieu/thong-ke-san-pham - Thống kê số lượng sản phẩm theo thương hiệu (chỉ Admin)
        [Authorize(Roles = "Admin")]
        [HttpGet("thong-ke-san-pham")]
        public async Task<IActionResult> GetProductStats()
        {
            var stats = await _context.Brands
                .Select(b => new BrandProductStatsDTO
                {
                    Name = b.Name,
                    ProductCount = b.Products.Count
                })
                .ToListAsync();

            return Ok(stats);
        }

        // GET: api/thuong-hieu/sap-xep - Sắp xếp thương hiệu theo tên hoặc số lượng sản phẩm
        [HttpGet("sap-xep")]
        public async Task<ActionResult<IEnumerable<Brand>>> GetSortedBrands(string sortBy = "name", bool ascending = true)
        {
            IQueryable<Brand> query = _context.Brands.Include(b => b.Products);

            query = sortBy.ToLower() switch
            {
                "name" => ascending ? query.OrderBy(b => b.Name) : query.OrderByDescending(b => b.Name),
                "productcount" => ascending ? query.OrderBy(b => b.Products.Count) : query.OrderByDescending(b => b.Products.Count),
                _ => query.OrderBy(b => b.Name) // Mặc định sắp xếp theo tên tăng dần
            };

            return await query.ToListAsync();
        }

        // GET: api/thuong-hieu/pho-bien - Lấy thương hiệu phổ biến dựa trên số lượng sản phẩm đã bán
        [HttpGet("pho-bien")]
        public async Task<IActionResult> GetPopularBrands(int top = 5)
        {
            var popularBrands = await _context.Brands
    .Select(b => new BrandPopularDTO
    {
        Name = b.Name,
        ProductSoldCount = b.Products
            .Sum(p => p.OrderDetails
                .Sum(od => od.Quantity ?? 0)) // Xử lý null cho Quantity
    })
    .OrderByDescending(b => b.ProductSoldCount)
    .Take(top)
    .ToListAsync();

            return Ok(popularBrands);
        }

        // GET: api/thuong-hieu/so-sanh-doanh-thu - So sánh doanh thu giữa các thương hiệu
        [HttpGet("so-sanh-doanh-thu")]
        public async Task<IActionResult> CompareBrandRevenue()
        {
            var brandRevenues = await _context.Brands
    .Select(b => new BrandRevenueDTO
    {
        Name = b.Name,
        TotalRevenue = b.Products
            .Sum(p => p.OrderDetails
                .Sum(od => (od.Quantity ?? 0) * (od.UnitPrice ?? 0))) // Xử lý null cho Quantity và UnitPrice
    })
    .OrderByDescending(b => b.TotalRevenue)
    .ToListAsync();

            return Ok(brandRevenues);
        }

        // GET: api/thuong-hieu/{id}/san-pham-moi - Lấy các sản phẩm mới nhất của thương hiệu
        [HttpGet("{id}/san-pham-moi")]
        public async Task<IActionResult> GetNewProductsByBrand(int id, int top = 5)
        {
            var newProducts = await _context.Products
                .Where(p => p.BrandId == id)
                .OrderByDescending(p => p.CreatedAt)
                .Take(top)
                .Select(p => new
                {
                    p.Name,
                    p.Price,
                    p.CreatedAt
                })
                .ToListAsync();

            return Ok(newProducts);
        }

        private bool BrandExists(int id)
        {
            return _context.Brands.Any(b => b.Id == id);
        }
        public class BrandCreateDTO
        {
            [Required(ErrorMessage = "Tên thương hiệu là bắt buộc.")]
            public string Name { get; set; }

            public string? Description { get; set; }

            public IFormFile? LogoFile { get; set; } // File logo tải lên
        }
        public class BrandUpdateDTO
        {
            [Required(ErrorMessage = "Tên thương hiệu là bắt buộc.")]
            public string Name { get; set; }

            public string? Description { get; set; }

            public IFormFile? LogoFile { get; set; } // File logo tải lên (nếu cần cập nhật)
        }
        public class BrandPagedListDTO
        {
            public int TotalBrands { get; set; }
            public int CurrentPage { get; set; }
            public int PageSize { get; set; }
            public IEnumerable<BrandSummaryDTO> Brands { get; set; } = new List<BrandSummaryDTO>();
        }
        public class BrandSummaryDTO
        {
            public int Id { get; set; }
            public string Name { get; set; } = null!;
            public string? LogoUrl { get; set; }
        }
        public class BrandProductStatsDTO
        {
            public string Name { get; set; } = null!;
            public int ProductCount { get; set; }
        }
        public class BrandPopularDTO
        {
            public string Name { get; set; } = null!;
            public int ProductSoldCount { get; set; }
        }
        public class BrandRevenueDTO
        {
            public string Name { get; set; } = null!;
            public decimal TotalRevenue { get; set; }
        }

    }
}