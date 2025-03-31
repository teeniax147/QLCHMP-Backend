using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuanLyCuaHangMyPham.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static QuanLyCuaHangMyPham.Controllers.OrdersController;

namespace QuanLyCuaHangMyPham.Services.ORDERS
{
    public interface IOrderService
    {
        Task<ActionResult<IEnumerable<Order>>> GetAllOrders();
        Task<IActionResult> GetPaginatedOrders(int pageNumber, int pageSize);
        Task<IActionResult> GetOrdersByCustomerId(int customerId);
        Task<IActionResult> GetOrdersByStatus(string status);
        Task<IActionResult> GetOrdersByDate(DateTime date);
        Task<IActionResult> GetOrderDetailsWithImages(int orderId);
        Task<IActionResult> GetOrderDetailsById(int orderId);
        Task<IActionResult> CreateOrder(int userId);
        Task<IActionResult> CreateGuestOrder(HttpContext httpContext);
        Task<IActionResult> CancelOrder(int orderId, string cancelReason);
        Task<IActionResult> UpdateStatus(int orderId, string status, string paymentStatus, DateTime? deliveryDate);
        Task<IActionResult> SearchOrders(string searchTerm, int pageNumber, int pageSize, string status);
    }
}