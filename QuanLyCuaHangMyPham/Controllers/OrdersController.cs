using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuanLyCuaHangMyPham.Facades;
using QuanLyCuaHangMyPham.Models;
using QuanLyCuaHangMyPham.Facades;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using QuanLyCuaHangMyPham.Data;
using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

namespace QuanLyCuaHangMyPham.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderFacade _orderFacade;
        private readonly ILogger<OrdersController> _logger;
        private readonly QuanLyCuaHangMyPhamContext _context;
        public OrdersController(QuanLyCuaHangMyPhamContext context, IOrderFacade orderFacade, ILogger<OrdersController> logger)
        {
            _orderFacade = orderFacade;
            _logger = logger;
            _context = context;
        }

        // GET: api/Orders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            return await _orderFacade.GetOrders();
        }

        // Lấy danh sách đơn hàng của tất cả khách hàng
        [Authorize(Roles = "Admin")]
        [HttpGet("all-orders")]
        public async Task<IActionResult> GetAllOrders([FromQuery] int pageNumber = 1, int pageSize = 10)
        {
            return await _orderFacade.GetAllOrders(pageNumber, pageSize);
        }

        // Lấy danh sách đơn hàng của một khách hàng cụ thể
        [Authorize]
        [HttpGet("customer/orders")]
        public async Task<IActionResult> GetOrdersByCustomer()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            return await _orderFacade.GetOrdersByCustomer(userId);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("filter-by-status")]
        public async Task<IActionResult> FilterOrdersByStatus(string status)
        {
            return await _orderFacade.FilterOrdersByStatus(status);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("by-date")]
        public async Task<IActionResult> GetOrdersByDate(DateTime date)
        {
            return await _orderFacade.GetOrdersByDate(date);
        }

        // Dùng để sử dụng cho reportcontroller
        [Authorize(Roles = "Admin")]
        [HttpGet("order-details/{orderId}")]
        public async Task<IActionResult> GetOrderDetailsById(int orderId)
        {
            return await _orderFacade.GetOrderDetailsById(orderId);
        }

        [Authorize]
        [HttpGet("orders/{orderId}/details")]
        public async Task<IActionResult> GetOrderDetailsWithImagesByOrderId(int orderId)
        {
            return await _orderFacade.GetOrderDetailsWithImages(orderId);
        }

        [HttpPost("create")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateOrder()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                _logger.LogInformation($"Controller: Tạo đơn hàng cho userId: {userId}");
                return await _orderFacade.CreateOrder(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Controller: Lỗi khi tạo đơn hàng");
                return StatusCode(500, new { Message = "Đã xảy ra lỗi khi xử lý đơn hàng." });
            }
        }

        [HttpPost("create-guest")]
        public async Task<IActionResult> CreateOrderForGuest()
        {
            // Sử dụng execution strategy của DbContext
            return await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                // Bắt đầu transaction
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Lấy thông tin từ session
                    var previewDataJson = HttpContext.Session.GetString("PreviewOrderData");
                    if (string.IsNullOrEmpty(previewDataJson))
                    {
                        return BadRequest(new { Message = "Không có dữ liệu đơn hàng tạm thời. Vui lòng thực hiện lại bước Preview." });
                    }

                    var previewData = JsonConvert.DeserializeObject<dynamic>(previewDataJson);

                    // Kiểm tra thông tin bắt buộc từ preview
                    var shippingAddress = (string)previewData.ShippingAddress;
                    var phoneNumber = (string)previewData.PhoneNumber;
                    var email = (string)previewData.Email;
                    if (string.IsNullOrWhiteSpace(shippingAddress))
                    {
                        return BadRequest(new { Message = "Địa chỉ giao hàng không được để trống." });
                    }

                    if (string.IsNullOrWhiteSpace(phoneNumber))
                    {
                        return BadRequest(new { Message = "Số điện thoại không được để trống." });
                    }

                    // Lấy giỏ hàng từ session
                    var cartItemsJson = HttpContext.Session.GetString("CartItems");
                    if (string.IsNullOrEmpty(cartItemsJson))
                    {
                        return BadRequest(new { Message = "Giỏ hàng trống. Vui lòng thêm sản phẩm." });
                    }

                    var cartItems = JsonConvert.DeserializeObject<List<CartItem>>(cartItemsJson);
                    if (cartItems == null || !cartItems.Any())
                    {
                        return BadRequest(new { Message = "Giỏ hàng trống. Vui lòng thêm sản phẩm." });
                    }

                    // Lấy thông tin sản phẩm sử dụng SQL thô
                    var productIds = cartItems.Select(ci => ci.ProductId).Distinct().ToList();
                    Dictionary<int, decimal> products;

                    using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                    {
                        await connection.OpenAsync();

                        var productQuery = $@"
                    SELECT id, price 
                    FROM Products 
                    WHERE id IN ({string.Join(",", productIds)})";

                        using (var command = new SqlCommand(productQuery, connection))
                        {
                            products = new Dictionary<int, decimal>();
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    products[reader.GetInt32(0)] = reader.GetDecimal(1);
                                }
                            }
                        }
                    }

                    // Tìm mã giảm giá nếu có
                    int? couponId = null;
                    string couponCode = (string)previewData.CouponCode;
                    if (!string.IsNullOrEmpty(couponCode))
                    {
                        var coupon = await _context.Coupons
                            .FirstOrDefaultAsync(c => c.Code == couponCode);

                        if (coupon != null && coupon.QuantityAvailable > 0)
                        {
                            couponId = coupon.Id;
                            // Giảm số lượng mã giảm giá
                            coupon.QuantityAvailable--;
                        }
                    }

                    // Tạo đơn hàng mới cho khách vãng lai
                    var order = new Order
                    {
                        CustomerId = 13, // Gán CustomerId mặc định là 13
                        CouponId = couponId,
                        PaymentMethodId = (int?)previewData.PaymentMethodId,
                        ShippingCompanyId = (int?)previewData.ShippingCompanyId,
                        ShippingAddress = shippingAddress,
                        PhoneNumber = phoneNumber,
                        Email = email,
                        OriginalTotalAmount = (decimal)previewData.OriginalTotalAmount,
                        TotalAmount = (decimal)previewData.TotalAmount,
                        ShippingCost = (decimal)previewData.ShippingCost,
                        DiscountApplied = (decimal)previewData.DiscountAmount,
                        OrderDate = DateTime.Now,
                        Status = "Chờ Xác Nhận",
                        PaymentStatus = "Chưa Thanh Toán"
                    };
                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    // Thêm chi tiết đơn hàng
                    var orderDetails = new List<OrderDetail>();
                    foreach (var item in cartItems)
                    {
                        if (!products.TryGetValue(item.ProductId, out decimal unitPrice))
                        {
                            throw new Exception($"Không tìm thấy giá cho sản phẩm ID {item.ProductId}");
                        }

                        orderDetails.Add(new OrderDetail
                        {
                            OrderId = order.Id,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = unitPrice,
                            TotalPrice = item.Quantity * unitPrice
                        });
                    }

                    _context.OrderDetails.AddRange(orderDetails);
                    await _context.SaveChangesAsync();

                    // Xóa giỏ hàng
                    HttpContext.Session.Remove("CartItems");
                    HttpContext.Session.Remove("PreviewOrderData");

                    // Commit transaction
                    await transaction.CommitAsync();

                    return Ok(new
                    {
                        Message = "Đơn hàng đã được tạo thành công.",
                        OrderId = order.Id
                    });
                }
                catch (Exception ex)
                {
                    // Rollback transaction
                    await transaction.RollbackAsync();

                    // Ghi log lỗi
                    Console.WriteLine($"Lỗi khi tạo đơn hàng cho khách: {ex}");

                    return StatusCode(500, new
                    {
                        Message = "Đã xảy ra lỗi trong quá trình tạo đơn hàng.",
                        Error = ex.Message
                    });
                }
            });
        }

        // Hủy đơn hàng (chỉ dành cho Customer, chỉ khi đơn hàng ở trạng thái Pending)
        [Authorize(Roles = "Customer")]
        [HttpPut("{orderId}/cancel")]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            return await _orderFacade.CancelOrder(orderId, userId, false);
        }

        // Cập nhật trạng thái đơn hàng (chỉ dành cho Admin)
        [Authorize(Roles = "Admin")]
        [HttpPut("{orderId}/update-status")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] UpdateOrderStatusRequest request)
        {
            return await _orderFacade.UpdateOrderStatus(orderId, request);
        }

        // Hủy đơn hàng (cho Staff và Admin)
        [Authorize(Roles = "Staff,Admin")]
        [HttpPut("{orderId}/cancel-by-staff")]
        public async Task<IActionResult> CancelOrderByStaff(int orderId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            return await _orderFacade.CancelOrder(orderId, userId, true);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("search")]
        public async Task<IActionResult> SearchOrders(
            string? searchTerm,
            int pageNumber = 1,
            int pageSize = 10,
            string? status = null)
        {
            return await _orderFacade.SearchOrders(searchTerm, pageNumber, pageSize, status);
        }

        // Các lớp DTO và Request/Response (giữ nguyên để tương thích)
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
            public string? Email { get; set; }
        }

        public class PreviewOrderGuestResponse
        {
            public string CouponCode { get; set; }
            public int PaymentMethodId { get; set; }
            public int ShippingCompanyId { get; set; }
            public string ShippingAddress { get; set; }
            public string PhoneNumber { get; set; }
            public string? Email { get; set; }
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
            public string? ShippingAddress { get; set; }
        }

        public class OrderDetailRequest
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal TotalPrice { get; set; }
        }

        public class UpdateOrderStatusRequest
        {
            public string Status { get; set; }
            public string PaymentStatus { get; set; }
            public DateTime? EstimatedDeliveryDate { get; set; }
        }

        public class OrderSummaryDto
        {
            public int OrderId { get; set; }
            public int CustomerId { get; set; }
            public string CustomerName { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal OriginalTotalAmount { get; set; }
            public DateTime OrderDate { get; set; }
            public string Status { get; set; }
            public string ShippingAddress { get; set; }
            public string ShippingMethod { get; set; }
            public string PaymentStatus { get; set; }
            public string PhoneNumber { get; set; }
            public string Email { get; set; }
        }

        public class OrderDetailDto
        {
            public int ProductId { get; set; }
            public string ProductVariation { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal TotalPrice { get; set; }
        }
    }
}