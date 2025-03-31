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

namespace QuanLyCuaHangMyPham.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderFacade _orderFacade;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrderFacade orderFacade, ILogger<OrdersController> logger)
        {
            _orderFacade = orderFacade;
            _logger = logger;
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
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            return await _orderFacade.CreateOrder(userId);
        }

        [HttpPost("create-guest")]
        public async Task<IActionResult> CreateOrderForGuest()
        {
            return await _orderFacade.CreateGuestOrder(HttpContext);
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