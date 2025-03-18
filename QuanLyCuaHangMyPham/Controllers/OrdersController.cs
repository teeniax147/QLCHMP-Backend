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
        //Lấy danh sách đơn hàng của tất cả khách hàng
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
                .ThenInclude(c => c.User)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.Coupon)
                .Include(o => o.ShippingCompany)
                .Include(o => o.PaymentMethod)
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
        //Lấy danh sách đơn hàng của một khách hàng cụ thể
        [Authorize]
        [HttpGet("customer/orders")]
        public async Task<IActionResult> GetOrdersByCustomer()
        {
            try
            {
                // Lấy UserId từ token
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Không thể xác định danh tính người dùng.");
                }

                int parsedUserId;
                if (!int.TryParse(userId, out parsedUserId))
                {
                    return BadRequest("UserId không hợp lệ.");
                }

                // Lấy thông tin khách hàng từ UserId
                var customer = await _context.Customers
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.UserId == parsedUserId);

                if (customer == null)
                {
                    return NotFound("Không tìm thấy thông tin khách hàng.");
                }

                // Lấy danh sách đơn hàng của khách hàng hiện tại
                var orders = await _context.Orders
                    .Where(o => o.CustomerId == customer.CustomerId)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .Include(o => o.Coupon)
                    .Include(o => o.ShippingCompany)
                    .Include(o => o.PaymentMethod)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                if (!orders.Any())
                {
                    return NotFound("Không có đơn hàng nào cho khách hàng này.");
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
                    CustomerName = $"{customer.User.FirstName} {customer.User.LastName}",
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
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách đơn hàng.", error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("orders/{orderId}/details")]
        public async Task<IActionResult> GetOrderDetailsWithImagesByOrderId(int orderId)
        {
            var orderDetails = await _context.OrderDetails
                .Include(od => od.Product) // Bao gồm thông tin sản phẩm
                .Where(od => od.OrderId == orderId) // Lọc theo OrderId
                .Select(od => new
                {
                    od.Id,
                    od.OrderId,
                    od.ProductId,
                    ProductName = od.Product.Name,
                    ProductDescription = od.Product.Description,
                    ProductImage = od.Product.ImageUrl, // Link hình ảnh sản phẩm
                    od.Quantity,
                    od.UnitPrice,
                    od.TotalPrice
                })
                .ToListAsync();

            if (!orderDetails.Any())
            {
                return NotFound(new { Message = "Không tìm thấy chi tiết sản phẩm cho đơn hàng này." });
            }

            return Ok(orderDetails);
        }


        [HttpPost("create")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateOrder()
        {
            // Tạo Execution Strategy cho DB
            var executionStrategy = _context.Database.CreateExecutionStrategy();

            // Thực hiện các thao tác trong phạm vi của một transaction
            await executionStrategy.ExecuteAsync(async () =>
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                        // Kiểm tra dữ liệu giỏ hàng trong cache
                        if (!_cache.TryGetValue($"PreviewOrder:{userId}", out var previewData))
                        {
                            return BadRequest("Không có dữ liệu đơn hàng tạm thời. Vui lòng thực hiện lại bước Preview.");
                        }

                        var previewOrder = JsonConvert.DeserializeObject<PreviewOrderResponse>(JsonConvert.SerializeObject(previewData));

                        // Lấy thông tin khách hàng từ cơ sở dữ liệu
                        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
                        if (customer == null)
                        {
                            return BadRequest("Không tìm thấy thông tin khách hàng.");
                        }

                        // Lấy các sản phẩm trong giỏ hàng của khách hàng
                        var cartItems = await _context.CartItems
                            .Include(ci => ci.Product)
                            .Where(ci => ci.Cart.CustomerId == customer.CustomerId)
                            .ToListAsync();

                        if (!cartItems.Any())
                        {
                            return BadRequest("Giỏ hàng trống.");
                        }

                        // Lấy thông tin Coupon và ShippingCompany (Tối ưu để giảm số lần query)
                        var coupon = string.IsNullOrEmpty(previewOrder.CouponCode)
                            ? null
                            : await _context.Coupons.FirstOrDefaultAsync(c => c.Code == previewOrder.CouponCode);

                        var shippingCompany = previewOrder.ShippingCompanyId != null
                            ? await _context.ShippingCompanies.FirstOrDefaultAsync(sc => sc.Id == previewOrder.ShippingCompanyId)
                            : null;

                        // Tạo đơn hàng mới
                        var order = new Order
                        {
                            CustomerId = customer.CustomerId,
                            CouponId = coupon?.Id,
                            PaymentMethodId = previewOrder.PaymentMethodId,
                            ShippingCompanyId = shippingCompany?.Id,
                            ShippingAddress = previewOrder.ShippingAddress ?? customer.User.Address,
                            PhoneNumber = previewOrder.PhoneNumber ?? customer.User.PhoneNumber,
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

                        if (order.Id <= 0)
                        {
                            return StatusCode(500, new { Message = "Không thể tạo ID cho đơn hàng." });
                        }

                        // Thêm các chi tiết đơn hàng vào database (tối ưu AddRange)
                        var orderDetails = cartItems.Select(item => new OrderDetail
                        {
                            OrderId = order.Id,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = item.Product.Price,
                            TotalPrice = item.Quantity * item.Product.Price
                        }).ToList();

                        _context.OrderDetails.AddRange(orderDetails);
                        await _context.SaveChangesAsync(); // Lưu OrderDetails

                        // Xóa các sản phẩm trong giỏ hàng sau khi tạo đơn hàng
                        _context.CartItems.RemoveRange(cartItems);
                        await _context.SaveChangesAsync();

                        // Commit transaction
                        await transaction.CommitAsync();

                        return Ok(new { Message = "Đơn hàng đã được tạo.", OrderId = order.Id });
                    }
                    catch (Exception ex)
                    {
                        // Rollback transaction in case of error
                        await transaction.RollbackAsync();
                        return StatusCode(500, new { Message = "Đã xảy ra lỗi trong quá trình tạo đơn hàng.", Error = ex.Message });
                    }
                }
            });

            return Ok(new { Message = "Đơn hàng đã được tạo."});
        }

        [HttpPost("create-guest")]
        public async Task<IActionResult> CreateOrderForGuest()
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Lấy thông tin từ session
                    var previewDataJson = HttpContext.Session.GetString("PreviewOrderData");
                    if (string.IsNullOrEmpty(previewDataJson))
                    {
                        return BadRequest("Không có dữ liệu đơn hàng tạm thời. Vui lòng thực hiện lại bước Preview.");
                    }

                    var previewData = JsonConvert.DeserializeObject<PreviewOrderGuestResponse>(previewDataJson);

                    // Tạo đơn hàng mới cho khách vãng lai (không cần tạo Customer mới)
                    var order = new Order
                    {
                        CustomerId = 13, // Gán CustomerId mặc định là 13
                        CouponId = string.IsNullOrEmpty(previewData.CouponCode)
    ? null
    : (await _context.Coupons.FirstOrDefaultAsync(c => c.Code == previewData.CouponCode))?.Id,
                        PaymentMethodId = previewData.PaymentMethodId,
                        ShippingCompanyId = previewData.ShippingCompanyId,
                        ShippingAddress = previewData.ShippingAddress,
                        PhoneNumber = previewData.PhoneNumber,
                        OriginalTotalAmount = previewData.OriginalTotalAmount,
                        TotalAmount = previewData.TotalAmount,
                        ShippingCost = previewData.ShippingCost,
                        DiscountApplied = previewData.DiscountAmount,
                        OrderDate = DateTime.Now,
                        Status = "Chờ Xác Nhận",
                        PaymentStatus = "Chưa Thanh Toán"
                    };

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    // Lấy giỏ hàng từ session
                    var cartItemsJson = HttpContext.Session.GetString("CartItems");
                    var cartItems = JsonConvert.DeserializeObject<List<CartItem>>(cartItemsJson);

                    // Thêm chi tiết đơn hàng
                    foreach (var item in cartItems)
                    {
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
                    HttpContext.Session.Remove("CartItems");
                    HttpContext.Session.Remove("PreviewOrderData");

                    await transaction.CommitAsync();

                    return Ok(new { Message = "Đơn hàng đã được tạo.", OrderId = order.Id });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { Message = "Đã xảy ra lỗi trong quá trình tạo đơn hàng.", Error = ex.Message });
                }
            }
        }
        // Hủy đơn hàng (chỉ dành cho Customer, chỉ khi đơn hàng ở trạng thái Pending)
        [Authorize(Roles = "Customer")]
        [HttpPut("{orderId}/cancel")]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound("Không tìm thấy đơn hàng.");
            }

            if (order.Status == "Chờ Xác Nhận")
            {
                order.Status = "Đã Hủy";
                order.PaymentStatus = "Đã Hủy. Sẽ hoàn tiền trong 24h đối với giao dịch chuyển khoản";

                // Cần lưu thay đổi vào database
                await _context.SaveChangesAsync();

                return Ok("Đơn hàng đã được hủy thành công.");
            }
            else
            {
                return BadRequest("Chỉ có thể hủy các đơn hàng đang chờ xác nhận.");
            }
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
            if (order.Status == "Đang Giao Hàng" || order.Status == "Đã Giao")
            {
                return BadRequest("Không thể hủy đơn hàng khi đang giao hàng hoặc đã hoàn thành.");
            }

            // Cập nhật trạng thái hủy đơn hàng
            order.Status = "Đã Hủy";
            order.PaymentStatus = "Đã Hủy. Sẽ hoàn tiền trong 24h đối với giao dịch chuyển khoản";
            await _context.SaveChangesAsync();

            return Ok("Đơn hàng đã được hủy bởi bên bán.");
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
            public string? PhoneNumber { get; set; }
        }
        public class PreviewOrderGuestResponse
        {
            public string CouponCode { get; set; }
            public int PaymentMethodId { get; set; }
            public int ShippingCompanyId { get; set; }
            public string ShippingAddress { get; set; }
            public string PhoneNumber { get; set; }
            public decimal OriginalTotalAmount { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal ShippingCost { get; set; }
            public decimal DiscountAmount { get; set; }
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
