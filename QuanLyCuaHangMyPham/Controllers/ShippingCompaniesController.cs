using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyCuaHangMyPham.Data;
using QuanLyCuaHangMyPham.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;

namespace QuanLyCuaHangMyPham.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShippingCompanyController : ControllerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;

        public ShippingCompanyController(QuanLyCuaHangMyPhamContext context)
        {
            _context = context;
        }

        // GET: api/ShippingCompany
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ShippingCompany>>> GetShippingCompanies()
        {
            return await _context.ShippingCompanies.ToListAsync();
        }

        // GET: api/ShippingCompany/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ShippingCompany>> GetShippingCompany(int id)
        {
            var shippingCompany = await _context.ShippingCompanies.FindAsync(id);

            if (shippingCompany == null)
            {
                return NotFound("Không tìm thấy công ty vận chuyển.");
            }

            return shippingCompany;
        }

        // PUT: api/ShippingCompany/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutShippingCompany(int id, ShippingCompanyUpdateRequest request)
        {
            if (id != request.Id)
            {
                return BadRequest("ID trong URL không khớp với ID trong yêu cầu.");
            }

            var shippingCompany = await _context.ShippingCompanies.FindAsync(id);
            if (shippingCompany == null)
            {
                return NotFound("Không tìm thấy công ty vận chuyển.");
            }

            shippingCompany.Name = request.Name;
            shippingCompany.ShippingCost = request.ShippingCost;
            shippingCompany.ImageUrl = request.ImageUrl;

            _context.Entry(shippingCompany).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ShippingCompanyExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/ShippingCompany
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ShippingCompany>> PostShippingCompany(ShippingCompanyCreateRequest request)
        {
            var shippingCompany = new ShippingCompany
            {
                Name = request.Name,
                ShippingCost = request.ShippingCost,
                ImageUrl = request.ImageUrl,
                CreatedAt = DateTime.Now
            };

            _context.ShippingCompanies.Add(shippingCompany);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetShippingCompany", new { id = shippingCompany.Id }, shippingCompany);
        }

        // DELETE: api/ShippingCompany/5
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteShippingCompany(int id)
        {
            var shippingCompany = await _context.ShippingCompanies.FindAsync(id);
            if (shippingCompany == null)
            {
                return NotFound("Không tìm thấy công ty vận chuyển.");
            }

            _context.ShippingCompanies.Remove(shippingCompany);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ShippingCompanyExists(int id)
        {
            return _context.ShippingCompanies.Any(e => e.Id == id);
        }

        // Các lớp request cho ShippingCompany
        public class ShippingCompanyCreateRequest
        {
            [Required]
            [MaxLength(255, ErrorMessage = "Tên công ty không được vượt quá 255 ký tự.")]
            public string Name { get; set; }

            [Required]
            [Range(0, double.MaxValue, ErrorMessage = "Chi phí vận chuyển phải lớn hơn hoặc bằng 0.")]
            public decimal ShippingCost { get; set; }

            public string? ImageUrl { get; set; }
        }

        public class ShippingCompanyUpdateRequest
        {
            [Required]
            public int Id { get; set; }

            [Required]
            [MaxLength(255, ErrorMessage = "Tên công ty không được vượt quá 255 ký tự.")]
            public string Name { get; set; }

            [Required]
            [Range(0, double.MaxValue, ErrorMessage = "Chi phí vận chuyển phải lớn hơn hoặc bằng 0.")]
            public decimal ShippingCost { get; set; }

            public string? ImageUrl { get; set; }
        }
    }
}