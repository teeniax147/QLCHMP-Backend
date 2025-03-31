using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuanLyCuaHangMyPham.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static QuanLyCuaHangMyPham.Controllers.OrdersController;

namespace QuanLyCuaHangMyPham.Facades
{
    public interface IOrderFacade
    {
        Task<ActionResult<IEnumerable<Order>>> GetOrders();
        Task<IActionResult> GetAllOrders(int pageNumber, int pageSize);
        Task<IActionResult> GetOrdersByCustomer(int userId);
        Task<IActionResult> FilterOrdersByStatus(string status);
        Task<IActionResult> GetOrdersByDate(DateTime date);
        Task<IActionResult> GetOrderDetailsWithImages(int orderId);
        Task<IActionResult> GetOrderDetailsById(int orderId);
        Task<IActionResult> CreateOrder(int userId);
        Task<IActionResult> CreateGuestOrder(HttpContext httpContext);
        Task<IActionResult> CancelOrder(int orderId, int userId, bool isAdmin);
        Task<IActionResult> UpdateOrderStatus(int orderId, UpdateOrderStatusRequest request);
        Task<IActionResult> SearchOrders(string searchTerm, int pageNumber, int pageSize, string status);
    }
}