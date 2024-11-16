using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Build.Framework;
using Microsoft.EntityFrameworkCore;
using QuanLyCuaHangMyPham.Data;
using QuanLyCuaHangMyPham.Models;

namespace QuanLyCuaHangMyPham.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminsController : ControllerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;

        public AdminsController(QuanLyCuaHangMyPhamContext context)
        {
            _context = context;
        }

        // GET: api/Admins
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Admin>>> GetAdmins()
        {
            return await _context.Admins.ToListAsync();
        }

        // GET: api/Admins/5
        [Authorize(Roles = "Admin")]
        [HttpGet("{id}")]
        public async Task<ActionResult<Admin>> GetAdmin(int id)
        {
            var admin = await _context.Admins.FindAsync(id);

            if (admin == null)
            {
                return BadRequest("Admin không tồn tại hoặc không đúng với ID.");
            }

            return admin;
        }

        // PUT: api/Admins/5
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAdmin(int id, AdminUpdateRequest request)
        {
            if (id != request.AdminId)
            {
                return BadRequest("ID Admin không khớp với ID trong trường yêu cầu.");
            }

            var admin = await _context.Admins.FindAsync(id);
            if (admin == null)
            {
                return BadRequest("Admin không tồn tại hoặc không đúng với ID.");
            }

            admin.UserId = request.UserId;
            admin.RoleDescription = request.RoleDescription;

            _context.Entry(admin).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AdminExists(id))
                {
                    return BadRequest("Admin đã tồn tại.");
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Admins
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<Admin>> PostAdmin(AdminCreateRequest request)
        {
            // Kiểm tra xem UserId đã tồn tại trong bảng Admins chưa
            var existingAdmin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == request.UserId);
            if (existingAdmin != null)
            {
                return Conflict(new { message = "Admin này đã tồn tại." });
            }

            // Nếu chưa tồn tại, tiến hành thêm mới
            var admin = new Admin
            {
                UserId = request.UserId,
                RoleDescription = request.RoleDescription
            };

            _context.Admins.Add(admin);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetAdmin", new { id = admin.AdminId }, admin);
        }

        // DELETE: api/Admins/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAdmin(int id)
        {
            var admin = await _context.Admins.FindAsync(id);
            if (admin == null)
            {
                return NotFound(new { message = $"Không tìm thấy admin với id = {id}." });
            }

            _context.Admins.Remove(admin);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa Admin!.", deletedAdmin = admin });
        }
        public class AdminCreateRequest
        {
            [Required]
            public int UserId { get; set; }

            public string? RoleDescription { get; set; }
        }
        public class AdminUpdateRequest
        {
            [Required]
            public int AdminId { get; set; }

            [Required]
            public int UserId { get; set; }

            public string? RoleDescription { get; set; }
        }
        private bool AdminExists(int id)
        {
            return _context.Admins.Any(e => e.AdminId == id);
        }
    }
}