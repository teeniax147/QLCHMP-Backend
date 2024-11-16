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
    public class MembershipLevelsController : ControllerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;

        public MembershipLevelsController(QuanLyCuaHangMyPhamContext context)
        {
            _context = context;
        }

        // GET: api/MembershipLevels
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MembershipLevel>>> GetMembershipLevels()
        {
            return await _context.MembershipLevels.ToListAsync();
        }

        // GET: api/MembershipLevels/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<MembershipLevel>> GetMembershipLevel(int id)
        {
            var membershipLevel = await _context.MembershipLevels.FindAsync(id);

            if (membershipLevel == null)
            {
                return NotFound();
            }

            return membershipLevel;
        }

        // PUT: api/MembershipLevels/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutMembershipLevel(int id, MembershipLevel membershipLevel)
        {
            if (id != membershipLevel.MembershipLevelId)
            {
                return BadRequest();
            }

            _context.Entry(membershipLevel).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MembershipLevelExists(id))
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

        // POST: api/MembershipLevels
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<MembershipLevel>> PostMembershipLevel(MembershipLevel membershipLevel)
        {
            _context.MembershipLevels.Add(membershipLevel);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetMembershipLevel", new { id = membershipLevel.MembershipLevelId }, membershipLevel);
        }

        // DELETE: api/MembershipLevels/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMembershipLevel(int id)
        {
            var membershipLevel = await _context.MembershipLevels.FindAsync(id);
            if (membershipLevel == null)
            {
                return NotFound();
            }

            _context.MembershipLevels.Remove(membershipLevel);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool MembershipLevelExists(int id)
        {
            return _context.MembershipLevels.Any(e => e.MembershipLevelId == id);
        }

    }
}
