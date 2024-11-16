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
    public class CustomersController : ControllerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;

        public CustomersController(QuanLyCuaHangMyPhamContext context)
        {
            _context = context;
        }

        // GET: api/Customers
        [Authorize]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers()
        {
            return await _context.Customers
                .Include(c => c.User) // Load thông tin người dùng liên quan
                .Include(c => c.MembershipLevel) // Load thông tin cấp độ thành viên
                .ToListAsync();
        }

        // GET: api/Customers/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<Customer>> GetCustomer(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.User)
                .Include(c => c.MembershipLevel)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
            {
                return NotFound();
            }

            return customer;
        }

        // PUT: api/Customers/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> PutCustomer(int id, CustomerUpdateRequest request)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            // Cập nhật thông tin từ request
            customer.Address = request.Address;
            customer.TotalSpending = request.TotalSpending;
            customer.MembershipLevelId = request.MembershipLevelId;

            _context.Entry(customer).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerExists(id))
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
        // POST: api/Customers
        [HttpPost]
        public async Task<ActionResult<Customer>> PostCustomer(CustomerCreateRequest request)
        {
            // Kiểm tra xem `UserId` có tồn tại trong bảng `AspNetUsers` không
            var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId);
            if (!userExists)
            {
                return BadRequest("UserId không tồn tại trong bảng AspNetUsers.");
            }

            // Tạo đối tượng Customer từ request
            var customer = new Customer
            {
                UserId = request.UserId,
                Address = request.Address,
                TotalSpending = request.TotalSpending,
                MembershipLevelId = request.MembershipLevelId, // Sử dụng giá trị từ request (mặc định là 1 nếu không cung cấp)
                CreatedAt = DateTime.Now // Thiết lập ngày tạo
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCustomer", new { id = customer.CustomerId }, customer);
        }

        // DELETE: api/Customers/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        public class CustomerCreateRequest
        {
            [Required]
            public int UserId { get; set; } // ID người dùng liên kết từ AspNetUsers

            [Required]
            [StringLength(255)]
            public string Address { get; set; } = string.Empty; // Địa chỉ khách hàng

            [Range(0, double.MaxValue)]
            public decimal TotalSpending { get; set; } = 0; // Tổng chi tiêu của khách hàng, mặc định là 0

            public int MembershipLevelId { get; set; } = 1; // ID cấp độ thành viên, mặc định là 1
        }

        public class CustomerUpdateRequest
        {
            [Required]
            [StringLength(255)]
            public string Address { get; set; } = string.Empty; // Địa chỉ khách hàng

            [Range(0, double.MaxValue)]
            public decimal TotalSpending { get; set; } = 0; // Tổng chi tiêu của khách hàng

            public int? MembershipLevelId { get; set; } // ID cấp độ thành viên (có thể null)
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.CustomerId == id);
        }
    }
}
