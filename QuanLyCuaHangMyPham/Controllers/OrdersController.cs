using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using QuanLyCuaHangMyPham.Data;
using QuanLyCuaHangMyPham.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore.Storage;
using static QuanLyCuaHangMyPham.Controllers.CartsController;

namespace QuanLyCuaHangMyPham.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;
        private readonly IMemoryCache _cache;  // Khai báo _cache
        public OrdersController(QuanLyCuaHangMyPhamContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // GET: api/Orders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            return await _context.Orders.ToListAsync();
        }

        // Lấy danh sách đơn hàng của người dùng
        [HttpGet("user-orders")]
        public async Task<IActionResult> GetUserOrders()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var orders = await _context.Orders
              .Include(o => o.Customer)
              .ThenInclude(c => c.User)
              .Include(o => o.OrderDetails)
              .ThenInclude(od => od.Product)
              .Include(o => o.Coupon)
              .Include(o => o.ShippingCompany)
              .Include(o => o.PaymentMethod)
              .OrderByDescending(o => o.OrderDate)
              .ToListAsync();

            if (!orders.Any())
            {
                return NotFound("Không có đơn hàng nào.");
            }

            return Ok(orders.Select(o => new
            {
                o.Id,
                o.OrderDate,
                o.Status,
                o.TotalAmount,
                o.DiscountApplied,
                o.ShippingCost,
                o.ShippingAddress,
                o.EstimatedDeliveryDate,
                OrderDetails = o.OrderDetails.Select(od => new
                {
                    od.ProductId,
                    od.Product.Name,
                    od.Quantity,
                    od.UnitPrice,
                    od.TotalPrice
                })
            }));
        }

        // Lấy chi tiết đơn hàng
        [Authorize]
        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrderDetails(int orderId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.Coupon)
                .Include(o => o.ShippingCompany)
                .Include(o => o.PaymentMethod)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == userId);

            if (order == null)
            {
                return NotFound("Không tìm thấy đơn hàng.");
            }

            return Ok(new
            {
                order.Id,
                order.OrderDate,
                order.Status,
                order.TotalAmount,
                order.DiscountApplied,
                order.ShippingCost,
                order.ShippingAddress,
                order.EstimatedDeliveryDate,
                order.PaymentStatus,
                order.OrderNotes,
                OrderDetails = order.OrderDetails.Select(od => new
                {
                    od.ProductId,
                    od.Product.Name,
                    od.Quantity,
                    od.UnitPrice,
                    od.TotalPrice
                })
            });
        }
        [HttpPost("create")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateOrder()
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                    if (!_cache.TryGetValue($"PreviewOrder:{userId}", out var previewData))
                    {
                        return BadRequest("Không có dữ liệu đơn hàng tạm thời. Vui lòng thực hiện lại bước Preview.");
                    }

                    var previewOrder = JsonConvert.DeserializeObject<PreviewOrderResponse>(JsonConvert.SerializeObject(previewData));

                    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
                    if (customer == null)
                    {
                        return BadRequest("Không tìm thấy thông tin khách hàng.");
                    }

                    var cartItems = await _context.CartItems
                        .Include(ci => ci.Product)
                        .Where(ci => ci.Cart.CustomerId == customer.CustomerId)
                        .ToListAsync();

                    if (!cartItems.Any())
                    {
                        return BadRequest("Giỏ hàng trống.");
                    }

                    // Tạo đơn hàng mới
                    var order = new Order
                    {
                        CustomerId = customer.CustomerId,
                        CouponId = string.IsNullOrEmpty(previewOrder.CouponCode)
                            ? null
                            : (await _context.Coupons.FirstOrDefaultAsync(c => c.Code == previewOrder.CouponCode))?.Id,
                        PaymentMethodId = previewOrder.PaymentMethodId,
                        ShippingCompanyId = previewOrder.ShippingCompanyId,
                        ShippingAddress = previewOrder.ShippingAddress ?? customer.User.Address,
                        OriginalTotalAmount = previewOrder.OriginalTotalAmount,
                        TotalAmount = previewOrder.TotalAmount,
                        ShippingCost = previewOrder.ShippingCost,
                        DiscountApplied = previewOrder.DiscountAmount,
                        OrderDate = DateTime.Now,
                        Status = "Chờ Xác Nhận",
                        PaymentStatus = "Chưa Thanh Toán"
                    };

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    // Kiểm tra lại Order.Id sau khi lưu
                    if (order.Id <= 0)
                    {
                        return StatusCode(500, new { Message = "Không thể tạo ID cho đơn hàng." });
                    }

                    // Thêm chi tiết đơn hàng
                    foreach (var item in cartItems)
                    {
                        if (item.Product == null)
                        {
                            return BadRequest("Sản phẩm không tồn tại trong giỏ hàng.");
                        }

                        _context.OrderDetails.Add(new OrderDetail
                        {
                            OrderId = order.Id,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = item.Product.Price,
                            TotalPrice = item.Quantity * item.Product.Price
                        });
                    }

                    await _context.SaveChangesAsync(); // Lưu OrderDetails sau khi lưu Order

                    // Xóa giỏ hàng
                    _context.CartItems.RemoveRange(cartItems);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return Ok(new { Message = "Đơn hàng đã được tạo thành công.", OrderId = order.Id });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { Message = "Đã xảy ra lỗi trong quá trình tạo đơn hàng.", Error = ex.Message });
                }
            }
        }

        // Hủy đơn hàng (chỉ dành cho Customer, chỉ khi đơn hàng ở trạng thái Pending)
        [Authorize]
        [HttpPut("{orderId}/cancel")]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == userId);

            if (order == null)
            {
                return NotFound("Không tìm thấy đơn hàng.");
            }

            if (order.Status != "Pending")
            {
                return BadRequest("Chỉ có thể hủy các đơn hàng đang chờ xử lý.");
            }

            order.Status = "Cancelled";
            order.PaymentStatus = "Refunded";
            await _context.SaveChangesAsync();

            return Ok("Đơn hàng đã được hủy thành công.");
        }

        // Cập nhật trạng thái đơn hàng (chỉ dành cho Admin)
        [Authorize(Roles = "Admin")]
        [HttpPut("{orderId}/update-status")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] UpdateOrderStatusRequest request)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound("Không tìm thấy đơn hàng.");
            }

            order.Status = request.Status;
            order.PaymentStatus = request.PaymentStatus;
            if (request.EstimatedDeliveryDate.HasValue)
            {
                order.EstimatedDeliveryDate = request.EstimatedDeliveryDate;
            }

            await _context.SaveChangesAsync();
            return Ok("Trạng thái đơn hàng đã được cập nhật.");
        }
        // Hủy đơn hàng (cho Staff và Admin)
        [Authorize(Roles = "Staff,Admin")]
        [HttpPut("{orderId}/cancel-by-staff")]
        public async Task<IActionResult> CancelOrderByStaff(int orderId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound("Không tìm thấy đơn hàng.");
            }

            // Kiểm tra trạng thái đơn hàng
            if (order.Status == "Đang Giao Hàng" || order.Status == "Hoàn Thành")
            {
                return BadRequest("Không thể hủy đơn hàng khi đang giao hàng hoặc đã hoàn thành.");
            }

            // Cập nhật trạng thái hủy đơn hàng
            order.Status = "Cancelled";
            order.PaymentStatus = "Refunded";
            await _context.SaveChangesAsync();

            return Ok("Đơn hàng đã được hủy bởi nhân viên hoặc quản trị viên.");
        }
        // Lấy danh sách tất cả đơn hàng (dành cho Admin)
        [Authorize(Roles = "Admin")]
        [HttpGet("all-orders")]
        public async Task<IActionResult> GetAllOrders([FromQuery] int pageNumber = 1, int pageSize = 10)
        {
            if (pageNumber <= 0 || pageSize <= 0)
            {
                return BadRequest("Số trang và số sản phẩm mỗi trang phải lớn hơn 0.");
            }

            var ordersQuery = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .OrderByDescending(o => o.OrderDate);

            var totalOrders = await ordersQuery.CountAsync();
            var orders = await ordersQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                TotalOrders = totalOrders,
                CurrentPage = pageNumber,
                PageSize = pageSize,
                Orders = orders.Select(o => new
                {
                    o.Id,
                    o.OrderDate,
                    o.Status,
                    o.TotalAmount,
                    o.DiscountApplied,
                    o.ShippingCost,
                    o.ShippingAddress,
                    o.EstimatedDeliveryDate,
                    CustomerName = $"{o.Customer.User.FirstName} {o.Customer.User.LastName}",
                    OrderDetails = o.OrderDetails.Select(od => new
                    {
                        od.ProductId,
                        od.Product.Name,
                        od.Quantity,
                        od.UnitPrice,
                        od.TotalPrice
                    })
                })
            });
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
        public class PreviewOrderResponse
        {
            public decimal OriginalTotalAmount { get; set; }
            public decimal DiscountAmount { get; set; }
            public decimal ShippingCost { get; set; }
            public decimal TotalAmount { get; set; }
            public string? CouponCode { get; set; }
            public int? ShippingCompanyId { get; set; }
            public int? PaymentMethodId { get; set; }
            public string? ShippingAddress { get; set; }
        }
        public class CreateOrderRequest
        {
            public string? CouponCode { get; set; }
            public int? PaymentMethodId { get; set; }
            public int? ShippingCompanyId { get; set; }
            public string? ShippingAddress { get; set; } // Cho phép khách hàng nhập địa chỉ khác nếu có
        }
        public class OrderDetailRequest
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal TotalPrice { get; set; }
        }

        // Lớp request cho cập nhật trạng thái đơn hàng
        public class UpdateOrderStatusRequest
        {
            public string Status { get; set; }
            public string PaymentStatus { get; set; }
            public DateTime? EstimatedDeliveryDate { get; set; }
        }
    }
}
