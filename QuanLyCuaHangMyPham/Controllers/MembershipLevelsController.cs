using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
                return NotFound("Không tìm thấy cấp độ thành viên.");
            }

            return membershipLevel;
        }

        // PUT: api/MembershipLevels/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutMembershipLevel(int id, UpdateMembershipLevelRequest request)
        {
            if (id != request.MembershipLevelId)
            {
                return BadRequest("ID trong URL không khớp với ID trong dữ liệu yêu cầu.");
            }

            var membershipLevel = await _context.MembershipLevels.FindAsync(id);
            if (membershipLevel == null)
            {
                return NotFound("Không tìm thấy cấp độ thành viên.");
            }

            membershipLevel.LevelName = request.LevelName;
            membershipLevel.MinimumSpending = request.MinimumSpending;
            membershipLevel.Benefits = request.Benefits;
            membershipLevel.DiscountRate = request.DiscountRate;

            _context.Entry(membershipLevel).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MembershipLevelExists(id))
                {
                    return NotFound("Không tìm thấy cấp độ thành viên.");
                }
                else
                {
                    throw;
                }
            }
            return Ok("Cập nhật cấp độ thành viên thành công.");
        }

        // POST: api/MembershipLevels
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<MembershipLevel>> PostMembershipLevel(CreateMembershipLevelRequest request)
        {
            var membershipLevel = new MembershipLevel
            {
                LevelName = request.LevelName,
                MinimumSpending = request.MinimumSpending,
                Benefits = request.Benefits,
                DiscountRate = request.DiscountRate,
                CreatedAt = DateTime.Now
            };

            _context.MembershipLevels.Add(membershipLevel);
            await _context.SaveChangesAsync();

            return Ok("Tạo cấp độ thành viên mới thành công.");
        }

        // DELETE: api/MembershipLevels/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMembershipLevel(int id)
        {
            var membershipLevel = await _context.MembershipLevels.FindAsync(id);
            if (membershipLevel == null)
            {
                return NotFound("Không tìm thấy cấp độ thành viên.");
            }

            _context.MembershipLevels.Remove(membershipLevel);
            await _context.SaveChangesAsync();

            return Ok("Xóa cấp độ thành viên thành công.");
        }
        [HttpGet("CustomerMembershipInfo")]
        [Authorize]
        public async Task<IActionResult> GetCustomerMembershipInfo()
        {
            // Lấy UserId từ token
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "Không thể xác định danh tính người dùng." });
            }

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return BadRequest(new { message = "UserId không hợp lệ." });
            }

            // Tìm customer dựa trên user ID
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
            {
                return NotFound(new { message = "Không tìm thấy thông tin khách hàng." });
            }

            // Get all membership levels sorted by minimum spending
            var membershipLevels = await _context.MembershipLevels
                .OrderBy(ml => ml.MinimumSpending)
                .ToListAsync();

            // Find current membership level
            var currentLevel = membershipLevels
                .OrderByDescending(ml => ml.MinimumSpending)
                .FirstOrDefault(ml => customer.TotalSpending >= ml.MinimumSpending);

            // Find next membership level
            var nextLevel = membershipLevels
                .Where(ml => ml.MinimumSpending > (currentLevel?.MinimumSpending ?? 0))
                .OrderBy(ml => ml.MinimumSpending)
                .FirstOrDefault();

            // Prepare response
            var response = new CustomerMembershipInfoResponse
            {
                CustomerId = customer.CustomerId,
                CurrentLevelName = currentLevel?.LevelName ?? "Thành Viên",
                TotalSpending = customer.TotalSpending ?? 0m,
                CurrentDiscountRate = currentLevel != null ? currentLevel.DiscountRate : 0m,
                NextLevelName = nextLevel?.LevelName,
                AmountToNextLevel = nextLevel != null
        ? Math.Max(0, nextLevel.MinimumSpending - (customer.TotalSpending ?? 0m))
        : 0m,
                NextLevelDiscountRate = nextLevel != null ? nextLevel.DiscountRate : 0m
            };

            return Ok(response);
        }
        private decimal ConvertToDecimal(decimal? value)
        {
            return value ?? 0m;
        }
        public class CustomerMembershipInfoResponse
        {
            public int CustomerId { get; set; }
            public string CurrentLevelName { get; set; }
            public decimal TotalSpending { get; set; }
            public decimal CurrentDiscountRate { get; set; }
            public string NextLevelName { get; set; }
            public decimal AmountToNextLevel { get; set; }
            public decimal NextLevelDiscountRate { get; set; }
        }
        private bool MembershipLevelExists(int id)
        {
            return _context.MembershipLevels.Any(e => e.MembershipLevelId == id);
        }
    }

    public class CreateMembershipLevelRequest
    {
        public string LevelName { get; set; }
        public decimal MinimumSpending { get; set; }
        public string Benefits { get; set; }
        public decimal DiscountRate { get; set; }
    }

    public class UpdateMembershipLevelRequest
    {
        public int MembershipLevelId { get; set; }
        public string LevelName { get; set; }
        public decimal MinimumSpending { get; set; }
        public string Benefits { get; set; }
        public decimal DiscountRate { get; set; }
    }
}