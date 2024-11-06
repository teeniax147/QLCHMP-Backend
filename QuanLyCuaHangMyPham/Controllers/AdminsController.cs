using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Admin>>> GetAdmins()
        {
            return await _context.Admins.ToListAsync();
        }

        // GET: api/Admins/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Admin>> GetAdmin(int id)
        {
            var admin = await _context.Admins.FindAsync(id);

            if (admin == null)
            {
                return NotFound();
            }

            return admin;
        }

        // PUT: api/Admins/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAdmin(int id, AdminUpdateRequest request)
        {
            if (id != request.AdminId)
            {
                return BadRequest("Admin ID in URL does not match the ID in the request body.");
            }

            var admin = await _context.Admins.FindAsync(id);
            if (admin == null)
            {
                return NotFound();
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
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Admins
        [HttpPost]
        public async Task<ActionResult<Admin>> PostAdmin(AdminCreateRequest request)
        {
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
        public async Task<IActionResult> DeleteAdmin(int id)
        {
            var admin = await _context.Admins.FindAsync(id);
            if (admin == null)
            {
                return NotFound();
            }

            _context.Admins.Remove(admin);
            await _context.SaveChangesAsync();

            return NoContent();
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