﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyCuaHangMyPham.Data;
using QuanLyCuaHangMyPham.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Framework;

namespace QuanLyCuaHangMyPham.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StaffController : ControllerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;

        public StaffController(QuanLyCuaHangMyPhamContext context)
        {
            _context = context;
        }

        // GET: api/Staff
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Staff>>> GetStaff()
        {
            return await _context.Staff.Include(s => s.User).ToListAsync();
        }

        // GET: api/Staff/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Staff>> GetStaff(int id)
        {
            var staff = await _context.Staff.Include(s => s.User).FirstOrDefaultAsync(s => s.StaffId == id);

            if (staff == null)
            {
                return NotFound("Không tìm thấy nhân viên.");
            }

            return staff;
        }

        // PUT: api/Staff/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutStaff(int id, StaffUpdateRequest request)
        {
            if (id != request.StaffId)
            {
                return BadRequest("ID trong URL không khớp với ID trong dữ liệu yêu cầu.");
            }

            var staff = await _context.Staff.FindAsync(id);
            if (staff == null)
            {
                return NotFound("Không tìm thấy nhân viên.");
            }

            staff.UserId = request.UserId;
            staff.Position = request.Position;
            staff.HireDate = request.HireDate;

            _context.Entry(staff).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StaffExists(id))
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

        // POST: api/Staff
        [HttpPost]
        public async Task<ActionResult<Staff>> PostStaff(StaffCreateRequest request)
        {
            var staff = new Staff
            {
                UserId = request.UserId,
                Position = request.Position,
                HireDate = request.HireDate
            };

            _context.Staff.Add(staff);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetStaff", new { id = staff.StaffId }, staff);
        }

        // DELETE: api/Staff/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStaff(int id)
        {
            var staff = await _context.Staff.FindAsync(id);
            if (staff == null)
            {
                return NotFound("Không tìm thấy nhân viên.");
            }

            _context.Staff.Remove(staff);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Kiểm tra nhân viên tồn tại
        private bool StaffExists(int id)
        {
            return _context.Staff.Any(e => e.StaffId == id);
        }

        // Request models
        public class StaffCreateRequest
        {
            [Required]
            public int UserId { get; set; }

            public string? Position { get; set; }

            public DateTime? HireDate { get; set; }
        }

        public class StaffUpdateRequest
        {
            [Required]
            public int StaffId { get; set; }

            [Required]
            public int UserId { get; set; }

            public string? Position { get; set; }

            public DateTime? HireDate { get; set; }
        }
    }
}