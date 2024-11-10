using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuanLyCuaHangMyPham.Data;
using QuanLyCuaHangMyPham.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyCuaHangMyPham.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentMethodController : ControllerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;
        private readonly ILogger<PaymentMethodController> _logger;

        public PaymentMethodController(QuanLyCuaHangMyPhamContext context, ILogger<PaymentMethodController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/PaymentMethod
        [HttpGet]
        public async Task<IActionResult> GetPaymentMethods()
        {
            var paymentMethods = await _context.PaymentMethods.ToListAsync();
            return Ok(paymentMethods);
        }

        // GET: api/PaymentMethod/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPaymentMethod(int id)
        {
            var paymentMethod = await _context.PaymentMethods.FindAsync(id);
            if (paymentMethod == null)
            {
                return NotFound(new { message = "Không tìm thấy phương thức thanh toán." });
            }
            return Ok(paymentMethod);
        }

        // POST: api/PaymentMethod
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreatePaymentMethod([FromBody] CreatePaymentMethodRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var paymentMethod = new PaymentMethod
            {
                Name = request.Name,
                Description = request.Description,
                ImageUrl = request.ImageUrl,
                CreatedAt = DateTime.Now
            };

            _context.PaymentMethods.Add(paymentMethod);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPaymentMethod), new { id = paymentMethod.Id }, paymentMethod);
        }

        // PUT: api/PaymentMethod/{id}
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePaymentMethod(int id, [FromBody] UpdatePaymentMethodRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var paymentMethod = await _context.PaymentMethods.FindAsync(id);
            if (paymentMethod == null)
            {
                return NotFound(new { message = "Không tìm thấy phương thức thanh toán." });
            }

            paymentMethod.Name = request.Name;
            paymentMethod.Description = request.Description;
            paymentMethod.ImageUrl = request.ImageUrl;

            _context.PaymentMethods.Update(paymentMethod);
            await _context.SaveChangesAsync();

            return Ok(paymentMethod);
        }

        // DELETE: api/PaymentMethod/{id}
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePaymentMethod(int id)
        {
            var paymentMethod = await _context.PaymentMethods.FindAsync(id);
            if (paymentMethod == null)
            {
                return NotFound(new { message = "Không tìm thấy phương thức thanh toán." });
            }

            _context.PaymentMethods.Remove(paymentMethod);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Phương thức thanh toán đã được xóa thành công." });
        }

        // GET: api/PaymentMethod/available
        [Authorize]
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailablePaymentMethods()
        {
            var availablePaymentMethods = await _context.PaymentMethods.ToListAsync();
            return Ok(availablePaymentMethods);
        }

        // POST: api/PaymentMethod/select
        [Authorize]
        [HttpPost("select")]
        public async Task<IActionResult> SelectPaymentMethod([FromBody] SelectPaymentMethodRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var paymentMethod = await _context.PaymentMethods.FindAsync(request.PaymentMethodId);
            if (paymentMethod == null)
            {
                return NotFound(new { message = "Không tìm thấy phương thức thanh toán." });
            }

            var order = await _context.Orders.FindAsync(request.OrderId);
            if (order == null)
            {
                return NotFound(new { message = "Không tìm thấy đơn hàng." });
            }

            order.PaymentMethodId = request.PaymentMethodId;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Phương thức thanh toán đã được chọn thành công." });
        }
    }

    // Request classes
    public class CreatePaymentMethodRequest
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class UpdatePaymentMethodRequest
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class SelectPaymentMethodRequest
    {
        public int PaymentMethodId { get; set; }
        [Required]
        public int OrderId { get; set; } // Đơn hàng mà người dùng đang chọn phương thức thanh toán
    }
}
