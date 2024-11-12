using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    public class ProductsController : ControllerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;

        public ProductsController(QuanLyCuaHangMyPhamContext context)
        {
            _context = context;
        }

        // GET: api/san-pham/danh-sach
        [HttpGet("danh-sach")]
        public async Task<ActionResult<IEnumerable<Product>>> GetAllProducts(int pageNumber = 1, int pageSize = 10)
        {
            if (pageNumber <= 0 || pageSize <= 0)
            {
                return BadRequest("Số trang và số sản phẩm mỗi trang phải lớn hơn 0.");
            }

            var totalProducts = await _context.Products.CountAsync();
            var products = await _context.Products
                
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                DanhSachSanPham = products,
                TongSoSanPham = totalProducts,
                SoTrang = pageNumber,
                SoSanPhamMoiTrang = pageSize
            });
        }
        // GET: api/san-pham/loc
        [HttpGet("loc")]
        public async Task<ActionResult<IEnumerable<Product>>> FilterProducts([FromQuery] FilterProductsRequest request)
        {
            if (request.PageNumber <= 0 || request.PageSize <= 0)
            {
                return BadRequest("Số trang và số sản phẩm mỗi trang phải lớn hơn 0.");
            }

            var products = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Inventories)
                .Include(p => p.Promotions)
                .AsQueryable();

            // Kiểm tra nếu không có bộ lọc nào được áp dụng
            bool noFiltersApplied = !request.MinPrice.HasValue &&
                                    !request.MaxPrice.HasValue &&
                                    !request.MinStock.HasValue &&
                                    !request.MaxStock.HasValue &&
                                    !request.IsOnSale.HasValue &&
                                    !request.BrandId.HasValue &&
                                    string.IsNullOrEmpty(request.SortByPrice);

            if (noFiltersApplied)
            {
                // Nếu không có bộ lọc, trả về danh sách đầy đủ (tương tự GetAllProducts)
                var totalProducts = await products.CountAsync();
                var pagedProducts = await products
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return Ok(new
                {
                    DanhSachSanPham = pagedProducts,
                    TongSoSanPham = totalProducts,
                    SoTrang = request.PageNumber,
                    SoSanPhamMoiTrang = request.PageSize
                });
            }

            // Áp dụng các bộ lọc nếu có
            if (request.MinPrice.HasValue)
            {
                products = products.Where(p => p.Price >= request.MinPrice.Value);
            }
            if (request.MaxPrice.HasValue)
            {
                products = products.Where(p => p.Price <= request.MaxPrice.Value);
            }

            if (request.MinStock.HasValue)
            {
                products = products.Where(p => p.Inventories.Sum(i => i.QuantityInStock) >= request.MinStock.Value);
            }
            if (request.MaxStock.HasValue)
            {
                products = products.Where(p => p.Inventories.Sum(i => i.QuantityInStock) <= request.MaxStock.Value);
            }

            if (request.IsOnSale.HasValue)
            {
                products = products.Where(p => p.Promotions.Any(pr => pr.StartDate <= DateTime.Now && pr.EndDate >= DateTime.Now) == request.IsOnSale.Value);
            }

            if (request.BrandId.HasValue)
            {
                products = products.Where(p => p.BrandId == request.BrandId.Value);
            }

            if (!string.IsNullOrEmpty(request.SortByPrice))
            {
                products = request.SortByPrice.ToLower() switch
                {
                    "asc" => products.Where(p => p.Price != null).OrderBy(p => p.Price),
                    "desc" => products.Where(p => p.Price != null).OrderByDescending(p => p.Price),
                    _ => products
                };
            }

            // Phân trang sau khi áp dụng bộ lọc
            var totalFilteredProducts = await products.CountAsync();
            var pagedFilteredProducts = await products
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return Ok(new
            {
                DanhSachSanPham = pagedFilteredProducts,
                TongSoSanPham = totalFilteredProducts,
                SoTrang = request.PageNumber,
                SoSanPhamMoiTrang = request.PageSize
            });
        }
        // GET: api/san-pham/theo-danh-muc/{categoryId}
        [HttpGet("theo-danh-muc/{categoryId}")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductsByCategory(int categoryId, int pageNumber = 1, int pageSize = 10)
        {
            if (pageNumber <= 0 || pageSize <= 0)
            {
                return BadRequest("Số trang và số sản phẩm mỗi trang phải lớn hơn 0.");
            }

            // Lọc sản phẩm theo categoryId
            var products = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Inventories)
                .Include(p => p.Promotions)
                .Where(p => p.Categories.Any(c => c.Id == categoryId))
                .AsQueryable();

            var totalProducts = await products.CountAsync();
            var pagedProducts = await products
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                DanhSachSanPham = pagedProducts,
                TongSoSanPham = totalProducts,
                SoTrang = pageNumber,
                SoSanPhamMoiTrang = pageSize
            });
        }

        // GET: api/san-pham/chi-tiet/{id}
        [HttpGet("chi-tiet/{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound("Không tìm thấy sản phẩm.");
            }

            return Ok(product);
        }


        // POST: api/san-pham/them-moi
        [Authorize(Roles = "Admin")]
        [HttpPost("them-moi")]
        public async Task<ActionResult<Product>> PostProduct([FromBody] CreateProductRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var product = new Product
            {
                Name = request.Name,
                Price = request.Price,
                OriginalPrice = request.OriginalPrice,
                Description = request.Description,
                ImageUrl = request.ImageUrl,
                BrandId = request.BrandId
            };

            try
            {
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                return CreatedAtAction("GetProduct", new { id = product.Id }, product);
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Không thể lưu sản phẩm mới.", error = ex.Message });
            }
        }
        // PUT: api/san-pham/cap-nhat/{id}
        [Authorize(Roles = "Admin")]
        [HttpPut("cap-nhat/{id}")]
        public async Task<IActionResult> PutProduct(int id, [FromBody] UpdateProductRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound("Không tìm thấy sản phẩm.");
            }

            product.Name = request.Name;
            product.Price = request.Price;
            product.OriginalPrice = request.OriginalPrice;
            product.Description = request.Description;
            product.ImageUrl = request.ImageUrl;
            product.BrandId = request.BrandId;

            try
            {
                await _context.SaveChangesAsync();
                return Ok("Cập nhật sản phẩm thành công.");
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Không thể cập nhật sản phẩm.", error = ex.Message });
            }
        }

        // DELETE: api/san-pham/xoa/{id}
        [Authorize(Roles = "Admin")]
        [HttpDelete("xoa/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound("Không tìm thấy sản phẩm để xóa.");
            }

            try
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                return Ok("Xóa sản phẩm thành công.");
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Không thể xóa sản phẩm.", error = ex.Message });
            }
        }

        // API Lấy sản phẩm bán chạy nhất
        [HttpGet("ban-chay-nhat")]
        public async Task<ActionResult<IEnumerable<Product>>> GetBestSellingProducts([FromQuery] BestSellingProductsRequest request)
        {
            var bestSellers = await _context.Products
                .Include(p => p.OrderDetails)
                .OrderByDescending(p => p.OrderDetails.Sum(od => od.Quantity))
                .Take(request.Top)
                .ToListAsync();

            return Ok(bestSellers);
        }

        // API Lấy sản phẩm tương tự dựa trên danh mục
        [HttpGet("tuong-tu/{productId}")]
        public async Task<ActionResult<IEnumerable<Product>>> GetSimilarProducts(int productId, [FromQuery] SimilarProductsRequest request)
        {
            var product = await _context.Products
                .Include(p => p.Categories)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null || product.Categories == null || !product.Categories.Any())
            {
                return NotFound("Không tìm thấy sản phẩm hoặc sản phẩm không có danh mục.");
            }

            var categoryIds = product.Categories.Select(c => c.Id).ToList();
            var similarProducts = await _context.Products
                .Include(p => p.Categories)
                .Where(p => p.Id != productId && p.Categories.Any(c => categoryIds.Contains(c.Id)))
                .Take(request.Top)
                .ToListAsync();

            return Ok(similarProducts);
        }
        // API Tìm kiếm sản phẩm theo từ khóa
        [HttpGet("tim-kiem")]
        public async Task<ActionResult<IEnumerable<Product>>> SearchProducts([FromQuery] SearchProductsRequest request)
        {
            if (string.IsNullOrEmpty(request.Keyword))
            {
                return BadRequest("Hãy nhập từ khóa tìm kiếm.");
            }

            var products = _context.Products
                .Where(p => p.Name.Contains(request.Keyword))
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize);

            var totalCount = await _context.Products.CountAsync(p => p.Name.Contains(request.Keyword));
            var pagedProducts = await products.ToListAsync();

            return Ok(new
            {
                DanhSachSanPham = pagedProducts,
                TongSoSanPham = totalCount,
                SoTrang = request.PageNumber,
                SoSanPhamMoiTrang = request.PageSize
            });
        }

        // Kiểm tra xem sản phẩm có tồn tại không
        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
        // Request cho API thêm mới sản phẩm
        public class CreateProductRequest
        {
            [Required(ErrorMessage = "Tên sản phẩm là bắt buộc.")]
            public string Name { get; set; }

            [Required(ErrorMessage = "Giá là bắt buộc.")]
            [Range(0, double.MaxValue, ErrorMessage = "Giá phải là số dương.")]
            public decimal Price { get; set; }

            [Range(0, double.MaxValue, ErrorMessage = "Giá gốc phải là số dương.")]
            public decimal OriginalPrice { get; set; }

            public string? Description { get; set; }
            public string? ImageUrl { get; set; }
            public int? BrandId { get; set; }
        }

        // Request cho API cập nhật sản phẩm
        public class UpdateProductRequest
        {
            [Required(ErrorMessage = "Tên sản phẩm là bắt buộc.")]
            public string Name { get; set; }

            [Required(ErrorMessage = "Giá là bắt buộc.")]
            [Range(0, double.MaxValue, ErrorMessage = "Giá phải là số dương.")]
            public decimal Price { get; set; }

            [Range(0, double.MaxValue, ErrorMessage = "Giá gốc phải là số dương.")]
            public decimal OriginalPrice { get; set; }

            public string? Description { get; set; }
            public string? ImageUrl { get; set; }
            public int? BrandId { get; set; }
        }

        // Request cho API lọc sản phẩm
        public class FilterProductsRequest
        {
            public int PageNumber { get; set; } = 1;
            public int PageSize { get; set; } = 10;
            public decimal? MinPrice { get; set; }
            public decimal? MaxPrice { get; set; }
            public int? MinStock { get; set; }
            public int? MaxStock { get; set; }
            public bool? IsOnSale { get; set; }
            public int? BrandId { get; set; }
            public string? SortByPrice { get; set; } // "asc" cho tăng dần, "desc" cho giảm dần
        }

        // Request cho API tìm kiếm sản phẩm
        public class SearchProductsRequest
        {
            [Required(ErrorMessage = "Vui lòng nhập từ khóa tìm kiếm.")]
            public string Keyword { get; set; }

            [Range(1, int.MaxValue, ErrorMessage = "Số trang phải lớn hơn 0.")]
            public int PageNumber { get; set; } = 1;

            [Range(1, int.MaxValue, ErrorMessage = "Số sản phẩm mỗi trang phải lớn hơn 0.")]
            public int PageSize { get; set; } = 10;
        }

        // Request cho API lấy sản phẩm tương tự
        public class SimilarProductsRequest
        {
            [Range(1, int.MaxValue, ErrorMessage = "Số lượng sản phẩm tương tự phải lớn hơn 0.")]
            public int Top { get; set; } = 5;
        }

        // Request cho API lấy sản phẩm bán chạy nhất
        public class BestSellingProductsRequest
        {
            [Range(1, int.MaxValue, ErrorMessage = "Số lượng sản phẩm bán chạy nhất phải lớn hơn 0.")]
            public int Top { get; set; } = 10;
        }
    }
}