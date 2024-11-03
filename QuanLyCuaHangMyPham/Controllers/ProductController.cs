using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        /// <summary>
        /// Lấy danh sách sản phẩm với tìm kiếm, sắp xếp và phân trang
        /// </summary>
        [HttpGet("danh-sach")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string searchString = null,
            [FromQuery] string sortOrder = "asc")
        {
            var products = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.Name.Contains(searchString));
            }

            products = sortOrder.ToLower() switch
            {
                "desc" => products.OrderByDescending(p => p.Price),
                _ => products.OrderBy(p => p.Price)
            };

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
                SoSanPhamMoiTrang = pageSize,
                ThuTuSapXep = sortOrder
            });
        }

        // GET: api/san-pham/chi-tiet/{id}
        /// <summary>
        /// Lấy chi tiết một sản phẩm dựa trên ID
        /// </summary>
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

        // PUT: api/san-pham/cap-nhat/{id}
        /// <summary>
        /// Cập nhật thông tin sản phẩm dựa trên ID
        /// </summary>
        [HttpPut("cap-nhat/{id}")]
        public async Task<IActionResult> PutProduct(int id, Product product)
        {
            if (id != product.Id)
            {
                return BadRequest("ID sản phẩm không khớp.");
            }

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound("Không tìm thấy sản phẩm để cập nhật.");
                }
                else
                {
                    ModelState.AddModelError("", "Lỗi đồng bộ dữ liệu. Vui lòng thử lại.");
                }
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Không thể lưu thay đổi. Vui lòng thử lại sau.");
            }

            return Ok("Cập nhật sản phẩm thành công.");
        }

        // POST: api/san-pham/them-moi
        /// <summary>
        /// Thêm một sản phẩm mới
        /// </summary>
        [HttpPost("them-moi")]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();
                    return CreatedAtAction("GetProduct", new { id = product.Id }, product);
                }
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Không thể lưu thay đổi. Vui lòng thử lại sau.");
            }

            return BadRequest(ModelState);
        }

        // DELETE: api/san-pham/xoa/{id}
        /// <summary>
        /// Xóa một sản phẩm theo ID
        /// </summary>
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
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Không thể xóa sản phẩm. Vui lòng thử lại sau.");
            }

            return BadRequest(ModelState);
        }

        // GET: api/san-pham/tim-kiem
        /// <summary>
        /// Tìm kiếm sản phẩm theo từ khóa
        /// </summary>
        [HttpGet("tim-kiem")]
        public async Task<ActionResult<IEnumerable<Product>>> SearchProducts([FromQuery] string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return BadRequest("Hãy nhập từ khóa tìm kiếm.");
            }

            var products = await _context.Products
                .Where(p => p.Name.Contains(keyword))
                .ToListAsync();

            return Ok(products);
        }

        // Kiểm tra xem sản phẩm có tồn tại không
        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}