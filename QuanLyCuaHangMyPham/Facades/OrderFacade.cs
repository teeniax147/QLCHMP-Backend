using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuanLyCuaHangMyPham.Data;
using QuanLyCuaHangMyPham.Facades;
using QuanLyCuaHangMyPham.Models;
using QuanLyCuaHangMyPham.Services.ORDERS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static QuanLyCuaHangMyPham.Controllers.OrdersController;

namespace QuanLyCuaHangMyPham.Services.ORDERS.Facades
{
    public class OrderFacade : IOrderFacade
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrderFacade> _logger;
        private readonly QuanLyCuaHangMyPhamContext _context;

        public OrderFacade(
            IOrderService orderService,
            ILogger<OrderFacade> logger,
            QuanLyCuaHangMyPhamContext context)
        {
            _orderService = orderService;
            _logger = logger;
            _context = context;
        }

        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            try
            {
                _logger.LogInformation("Gọi GetOrders từ Facade");
                return await _orderService.GetAllOrders();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách đơn hàng");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetAllOrders(int pageNumber, int pageSize)
        {
            try
            {
                _logger.LogInformation($"Gọi GetAllOrders với pageNumber={pageNumber}, pageSize={pageSize} từ Facade");
                return await _orderService.GetPaginatedOrders(pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách đơn hàng phân trang");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetOrdersByCustomer(int userId)
        {
            try
            {
                _logger.LogInformation($"Gọi GetOrdersByCustomer cho userId={userId} từ Facade");
                // Lấy thông tin khách hàng từ UserId
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (customer == null)
                {
                    return new NotFoundObjectResult("Không tìm thấy thông tin khách hàng.");
                }

                return await _orderService.GetOrdersByCustomerId(customer.CustomerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy đơn hàng cho userId={userId}");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> FilterOrdersByStatus(string status)
        {
            try
            {
                _logger.LogInformation($"Gọi FilterOrdersByStatus với status={status} từ Facade");
                return await _orderService.GetOrdersByStatus(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lọc đơn hàng theo status={status}");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetOrdersByDate(DateTime date)
        {
            try
            {
                _logger.LogInformation($"Gọi GetOrdersByDate với date={date} từ Facade");
                return await _orderService.GetOrdersByDate(date);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy đơn hàng theo ngày {date}");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetOrderDetailsWithImages(int orderId)
        {
            try
            {
                _logger.LogInformation($"Gọi GetOrderDetailsWithImages cho orderId={orderId} từ Facade");
                return await _orderService.GetOrderDetailsWithImages(orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy chi tiết đơn hàng có hình ảnh cho orderId={orderId}");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetOrderDetailsById(int orderId)
        {
            try
            {
                _logger.LogInformation($"Gọi GetOrderDetailsById cho orderId={orderId} từ Facade");
                return await _orderService.GetOrderDetailsById(orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy chi tiết đơn hàng orderId={orderId}");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> CreateOrder(int userId)
        {
            try
            {
                _logger.LogInformation($"Gọi CreateOrder cho userId={userId} từ Facade");
                return await _orderService.CreateOrder(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tạo đơn hàng cho userId={userId}");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> CreateGuestOrder(HttpContext httpContext)
        {
            try
            {
                _logger.LogInformation("Gọi CreateGuestOrder từ Facade");
                return await _orderService.CreateGuestOrder(httpContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo đơn hàng cho khách vãng lai");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> CancelOrder(int orderId, int userId, bool isAdmin)
        {
            try
            {
                _logger.LogInformation($"Gọi CancelOrder cho orderId={orderId}, userId={userId}, isAdmin={isAdmin} từ Facade");

                // Nếu không phải admin, kiểm tra xem đơn hàng có thuộc về người dùng không
                if (!isAdmin)
                {
                    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
                    var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

                    if (customer == null || order == null || order.CustomerId != customer.CustomerId)
                    {
                        return new ForbidResult("Bạn không có quyền hủy đơn hàng này.");
                    }
                }

                string reason = isAdmin ? "Đơn hàng bị hủy bởi admin" : "Đơn hàng bị hủy bởi khách hàng";
                return await _orderService.CancelOrder(orderId, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi hủy đơn hàng orderId={orderId}");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> UpdateOrderStatus(int orderId, UpdateOrderStatusRequest request)
        {
            try
            {
                _logger.LogInformation($"Gọi UpdateOrderStatus cho orderId={orderId}, status={request.Status} từ Facade");
                return await _orderService.UpdateStatus(orderId, request.Status, request.PaymentStatus, request.EstimatedDeliveryDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi cập nhật trạng thái đơn hàng orderId={orderId}");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> SearchOrders(string searchTerm, int pageNumber, int pageSize, string status)
        {
            try
            {
                _logger.LogInformation($"Gọi SearchOrders với searchTerm={searchTerm}, pageNumber={pageNumber}, pageSize={pageSize}, status={status} từ Facade");
                return await _orderService.SearchOrders(searchTerm, pageNumber, pageSize, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tìm kiếm đơn hàng");
                return new StatusCodeResult(500);
            }
        }
    }
}