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
    public class OrdersController : ControllerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;

        public OrdersController(QuanLyCuaHangMyPhamContext context)
        {
            _context = context;
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
        [Authorize(Roles = "Customer")]
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
{
    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

    // Lấy thông tin khách hàng từ tài khoản
    var customer = await _context.Customers
        .Include(c => c.User)
        .FirstOrDefaultAsync(c => c.UserId == userId);

    if (customer == null)
    {
        return BadRequest("Không tìm thấy thông tin khách hàng.");
    }

    // Tính toán tổng tiền và các chi phí khác
    var cartItems = await _context.CartItems
        .Include(ci => ci.Product)
        .Where(ci => ci.Cart.CustomerId == customer.CustomerId)
        .ToListAsync();

    if (!cartItems.Any())
    {
        return BadRequest("Giỏ hàng trống.");
    }

    // Tổng giá trị ban đầu của đơn hàng
    decimal originalTotalAmount = cartItems.Sum(ci => ci.Product.Price * ci.Quantity);

    // Tính giảm giá nếu có mã giảm giá
    decimal discountAmount = 0;
    if (!string.IsNullOrEmpty(request.CouponCode))
    {
                var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == request.CouponCode);
                if (coupon == null || coupon.QuantityAvailable <= 0 ||
        (coupon.StartDate.HasValue && coupon.StartDate > DateOnly.FromDateTime(DateTime.Now)) ||
        (coupon.EndDate.HasValue && coupon.EndDate < DateOnly.FromDateTime(DateTime.Now)))
                {
            return BadRequest("Mã giảm giá không hợp lệ hoặc đã hết hạn.");
        }

        discountAmount = coupon.DiscountAmount ?? (coupon.DiscountPercentage.HasValue
            ? originalTotalAmount * (coupon.DiscountPercentage.Value / 100)
            : 0);

        if (coupon.MaxDiscountAmount.HasValue && discountAmount > coupon.MaxDiscountAmount.Value)
        {
            discountAmount = coupon.MaxDiscountAmount.Value;
        }
    }

    // Tổng tiền sau khi áp dụng giảm giá
    decimal totalAmount = originalTotalAmount - discountAmount;

    // Tính phí vận chuyển nếu có công ty vận chuyển
    decimal shippingCost = 0;
    if (request.ShippingCompanyId.HasValue)
    {
        var shippingCompany = await _context.ShippingCompanies
            .FirstOrDefaultAsync(sc => sc.Id == request.ShippingCompanyId.Value);

        if (shippingCompany != null)
        {
            shippingCost = shippingCompany.ShippingCost ?? 0;
            totalAmount += shippingCost;
        }
    }

    // Tạo đơn hàng mới
    var order = new Order
    {
        CustomerId = customer.CustomerId,
        CouponId = string.IsNullOrEmpty(request.CouponCode) ? null : (await _context.Coupons.FirstOrDefaultAsync(c => c.Code == request.CouponCode))?.Id,
        PaymentMethodId = request.PaymentMethodId,
        OriginalTotalAmount = originalTotalAmount,
        TotalAmount = totalAmount,
        ShippingAddress = string.IsNullOrEmpty(request.ShippingAddress) ? customer.User.Address : request.ShippingAddress,
        ShippingCompanyId = request.ShippingCompanyId,
        ShippingCost = shippingCost,
        DiscountApplied = discountAmount,
        OrderDate = DateTime.Now,
        Status = "Pending",  // Trạng thái mặc định khi tạo đơn hàng
        PaymentStatus = "Unpaid" // Trạng thái thanh toán mặc định
    };

    _context.Orders.Add(order);
    await _context.SaveChangesAsync();

    // Thêm chi tiết đơn hàng
    foreach (var item in cartItems)
    {
        var orderDetail = new OrderDetail
        {
            OrderId = order.Id,
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            UnitPrice = item.Product.Price,
            TotalPrice = item.Quantity * item.Product.Price
        };

        _context.OrderDetails.Add(orderDetail);
    }

    // Lưu thay đổi và xóa các sản phẩm đã mua khỏi giỏ hàng
    _context.CartItems.RemoveRange(cartItems);
    await _context.SaveChangesAsync();

    return Ok(new
    {
        Message = "Đơn hàng đã được tạo thành công.",
        OrderId = order.Id
    });
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
