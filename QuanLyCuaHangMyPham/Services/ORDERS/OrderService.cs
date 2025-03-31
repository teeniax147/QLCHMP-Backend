using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using QuanLyCuaHangMyPham.Data;
using QuanLyCuaHangMyPham.Models;
using QuanLyCuaHangMyPham.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static QuanLyCuaHangMyPham.Controllers.OrdersController;

namespace QuanLyCuaHangMyPham.Services.ORDERS
{
    public class OrderService : IOrderService
    {
        private readonly QuanLyCuaHangMyPhamContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<OrderService> _logger;
        private readonly OrderStateContext _stateContext;

        public OrderService(
            QuanLyCuaHangMyPhamContext context,
            IMemoryCache cache,
            ILogger<OrderService> logger,
            OrderStateContext stateContext)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
            _stateContext = stateContext;
        }

        // ---------- PHƯƠNG THỨC GET ----------

        public async Task<ActionResult<IEnumerable<Order>>> GetAllOrders()
        {
            try
            {
                var orders = await _context.Orders.ToListAsync();
                return orders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy tất cả đơn hàng");
                throw;
            }
        }

        public async Task<IActionResult> GetPaginatedOrders(int pageNumber, int pageSize)
        {
            try
            {
                if (pageNumber <= 0 || pageSize <= 0)
                {
                    return new BadRequestObjectResult("Số trang và số sản phẩm mỗi trang phải lớn hơn 0.");
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
                    .Select(o => new OrderSummaryDto
                    {
                        OrderId = o.Id,
                        CustomerId = o.CustomerId,
                        CustomerName = $"{o.Customer.User.FirstName} {o.Customer.User.LastName}",
                        TotalAmount = o.TotalAmount ?? 0m,
                        OriginalTotalAmount = o.OriginalTotalAmount ?? 0m,
                        OrderDate = o.OrderDate.Value,
                        Status = o.Status,
                        ShippingAddress = o.ShippingAddress,
                        ShippingMethod = o.ShippingMethod,
                        PaymentStatus = o.PaymentStatus,
                        PhoneNumber = o.PhoneNumber,
                        Email = o.Email,
                    })
                    .ToListAsync();

                return new OkObjectResult(new
                {
                    TotalOrders = totalOrders,
                    CurrentPage = pageNumber,
                    PageSize = pageSize,
                    Orders = orders
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi phân trang đơn hàng");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetOrdersByCustomerId(int customerId)
        {
            try
            {
                // Lấy thông tin khách hàng từ CustomerId
                var customer = await _context.Customers
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.CustomerId == customerId);

                if (customer == null)
                {
                    return new NotFoundObjectResult("Không tìm thấy thông tin khách hàng.");
                }

                // Lấy danh sách đơn hàng của khách hàng
                var orders = await _context.Orders
                    .Where(o => o.CustomerId == customerId)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .Include(o => o.Coupon)
                    .Include(o => o.ShippingCompany)
                    .Include(o => o.PaymentMethod)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                if (!orders.Any())
                {
                    return new NotFoundObjectResult("Không có đơn hàng nào cho khách hàng này.");
                }

                var result = orders.Select(o => new
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
                    PaymentMethodName = o.PaymentMethod?.Name,
                    ShippingCompanyName = o.ShippingCompany?.Name,
                    OrderDetails = o.OrderDetails.Select(od => new
                    {
                        od.ProductId,
                        od.Product.Name,
                        od.Quantity,
                        od.UnitPrice,
                        od.TotalPrice
                    })
                });

                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách đơn hàng của khách hàng");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetOrdersByStatus(string status)
        {
            try
            {
                // Kiểm tra giá trị status hợp lệ
                if (!new[] { "Chờ Xác Nhận", "Chờ Lấy Hàng", "Đang Giao Hàng", "Đã Giao", "Đã Hủy" }.Contains(status))
                {
                    return new BadRequestObjectResult("Trạng thái không hợp lệ.");
                }

                var orders = await _context.Orders
                    .Where(o => o.Status == status)
                    .Include(o => o.Customer)
                        .ThenInclude(c => c.User)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .Include(o => o.Coupon)
                    .Include(o => o.ShippingCompany)
                    .Include(o => o.PaymentMethod)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                var result = orders.Select(o => new
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
                });

                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy đơn hàng theo trạng thái: {status}");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetOrdersByDate(DateTime date)
        {
            try
            {
                var ordersOnDate = await _context.Orders
                    .Where(o => o.OrderDate.HasValue && o.OrderDate.Value.Date == date.Date)
                    .Select(o => new OrderSummaryDto
                    {
                        OrderId = o.Id,
                        CustomerId = o.CustomerId,
                        CustomerName = _context.Users
                            .Where(u => u.Id == o.Customer.UserId)
                            .Select(u => $"{u.FirstName} {u.LastName}")
                            .FirstOrDefault(),
                        TotalAmount = o.TotalAmount ?? 0m,
                        OriginalTotalAmount = o.OriginalTotalAmount ?? 0m,
                        OrderDate = o.OrderDate.Value,
                        Status = o.Status,
                        ShippingAddress = o.ShippingAddress,
                        ShippingMethod = o.ShippingMethod,
                        PaymentStatus = o.PaymentStatus,
                        PhoneNumber = o.PhoneNumber,
                        Email = o.Email
                    })
                    .ToListAsync();

                return new OkObjectResult(ordersOnDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy đơn hàng theo ngày: {date}");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetOrderDetailsWithImages(int orderId)
        {
            try
            {
                var orderDetails = await _context.OrderDetails
                    .Include(od => od.Product)
                    .Where(od => od.OrderId == orderId)
                    .Select(od => new
                    {
                        od.Id,
                        od.OrderId,
                        od.ProductId,
                        ProductName = od.Product.Name,
                        ProductDescription = od.Product.Description,
                        ProductImage = od.Product.ImageUrl,
                        od.Quantity,
                        od.UnitPrice,
                        od.TotalPrice
                    })
                    .ToListAsync();

                if (!orderDetails.Any())
                {
                    return new NotFoundObjectResult("Không tìm thấy chi tiết sản phẩm cho đơn hàng này.");
                }

                return new OkObjectResult(orderDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy chi tiết đơn hàng với hình ảnh, orderId: {orderId}");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> GetOrderDetailsById(int orderId)
        {
            try
            {
                var orderInfo = await _context.Orders
                    .Where(o => o.Id == orderId)
                    .Select(o => new
                    {
                        OrderId = o.Id,
                        CustomerId = o.CustomerId,
                        CustomerName = _context.Users
                            .Where(u => u.Id == o.Customer.UserId)
                            .Select(u => $"{u.FirstName} {u.LastName}")
                            .FirstOrDefault(),
                        TotalAmount = o.TotalAmount ?? 0m,
                        OriginalTotalAmount = o.OriginalTotalAmount ?? 0m,
                        OrderDate = o.OrderDate,
                        Status = o.Status,
                        ShippingAddress = o.ShippingAddress,
                        ShippingMethod = o.ShippingMethod,
                        PaymentStatus = o.PaymentStatus,
                        PhoneNumber = o.PhoneNumber,
                        Email = o.Email,
                        OrderNotes = o.OrderNotes,
                        EstimatedDeliveryDate = o.EstimatedDeliveryDate
                    })
                    .FirstOrDefaultAsync();

                if (orderInfo == null)
                {
                    return new NotFoundObjectResult("Không tìm thấy đơn hàng.");
                }

                var orderDetails = await _context.OrderDetails
                    .Where(od => od.OrderId == orderId)
                    .Select(od => new OrderDetailDto
                    {
                        ProductId = od.ProductId,
                        ProductVariation = od.ProductVariation,
                        Quantity = od.Quantity ?? 0,
                        UnitPrice = od.UnitPrice ?? 0m,
                        TotalPrice = od.TotalPrice ?? 0m
                    })
                    .ToListAsync();

                return new OkObjectResult(new
                {
                    OrderInfo = orderInfo,
                    OrderDetails = orderDetails
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy chi tiết đơn hàng theo ID: {orderId}");
                return new StatusCodeResult(500);
            }
        }

        // ---------- PHƯƠNG THỨC SEARCH ----------

        public async Task<IActionResult> SearchOrders(string searchTerm, int pageNumber, int pageSize, string status)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 50) pageSize = 10;

                // Bắt đầu với truy vấn IQueryable
                var query = _context.Orders
                    .Include(o => o.Customer)
                        .ThenInclude(c => c.User)
                    .AsQueryable();

                // Kiểm tra nếu searchTerm là số (ID đơn hàng)
                int orderId = 0;
                bool isOrderId = false;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    isOrderId = int.TryParse(searchTerm, out orderId);
                    searchTerm = searchTerm.Trim().ToLower();

                    // Áp dụng tìm kiếm
                    query = query.Where(o =>
                        // Tìm theo tên khách hàng
                        ((o.Customer.User.FirstName ?? "") + " " + (o.Customer.User.LastName ?? "")).ToLower().Contains(searchTerm) ||
                        // Tìm theo PhoneNumber
                        (o.PhoneNumber != null && o.PhoneNumber.ToLower().Contains(searchTerm)) ||
                        // Tìm theo Email
                        (o.Email != null && o.Email.ToLower().Contains(searchTerm)) ||
                        // Hoặc tìm theo ID đơn hàng
                        (isOrderId && o.Id == orderId)
                    );
                }

                // Lọc theo trạng thái nếu được chỉ định
                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(o => o.Status != null && o.Status.ToLower() == status.ToLower());
                }

                // Đếm tổng số kết quả để tính phân trang
                var totalOrders = await query.CountAsync();

                // Lấy danh sách đơn hàng
                var orderEntities = await query
                    .OrderByDescending(o => o.OrderDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Map sang DTO
                var orders = orderEntities.Select(o => new OrderSummaryDto
                {
                    OrderId = o.Id,
                    CustomerId = o.CustomerId,
                    CustomerName = o.Customer != null ?
                        $"{o.Customer.User?.FirstName ?? ""} {o.Customer.User?.LastName ?? ""}".Trim() :
                        "",
                    TotalAmount = o.TotalAmount ?? 0m,
                    OriginalTotalAmount = o.OriginalTotalAmount ?? 0m,
                    OrderDate = o.OrderDate ?? DateTime.Now,
                    Status = o.Status,
                    ShippingAddress = o.ShippingAddress,
                    ShippingMethod = o.ShippingMethod,
                    PaymentStatus = o.PaymentStatus,
                    PhoneNumber = o.PhoneNumber,
                    Email = o.Email
                }).ToList();

                return new OkObjectResult(new
                {
                    TotalOrders = totalOrders,
                    CurrentPage = pageNumber,
                    PageSize = pageSize,
                    Orders = orders
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tìm kiếm đơn hàng");
                return new StatusCodeResult(500);
            }
        }

        // ---------- PHƯƠNG THỨC CREATE/UPDATE ----------

        public async Task<IActionResult> CreateOrder(int userId)
        {
            // Tạo Execution Strategy cho DB
            var executionStrategy = _context.Database.CreateExecutionStrategy();

            // Thực hiện các thao tác trong phạm vi của một transaction
            return await executionStrategy.ExecuteAsync(
                async () => await CreateOrderInternal(userId));
        }

        // Phương thức nội bộ để xử lý logic tạo đơn hàng
        private async Task<IActionResult> CreateOrderInternal(int userId)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Kiểm tra dữ liệu giỏ hàng trong cache
                    if (!_cache.TryGetValue($"PreviewOrder:{userId}", out var previewData))
                    {
                        return new BadRequestObjectResult("Không có dữ liệu đơn hàng tạm thời. Vui lòng thực hiện lại bước Preview.");
                    }

                    var previewOrder = JsonConvert.DeserializeObject<PreviewOrderResponse>(
                        JsonConvert.SerializeObject(previewData));

                    // Lấy thông tin khách hàng từ cơ sở dữ liệu
                    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
                    if (customer == null)
                    {
                        return new BadRequestObjectResult("Không tìm thấy thông tin khách hàng.");
                    }

                    // Lấy các sản phẩm trong giỏ hàng của khách hàng
                    var cartItems = await _context.CartItems
                        .Include(ci => ci.Product)
                        .Where(ci => ci.Cart.CustomerId == customer.CustomerId)
                        .ToListAsync();

                    if (!cartItems.Any())
                    {
                        return new BadRequestObjectResult("Giỏ hàng trống.");
                    }

                    // Lấy thông tin Coupon và ShippingCompany
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
                        Email = previewOrder.Email ?? customer.User.Email,
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
                        return new StatusCodeResult(500);
                    }

                    // Thêm các chi tiết đơn hàng vào database
                    var orderDetails = cartItems.Select(item => new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Product.Price,
                        TotalPrice = item.Quantity * item.Product.Price
                    }).ToList();

                    _context.OrderDetails.AddRange(orderDetails);
                    await _context.SaveChangesAsync();

                    // Xóa các sản phẩm trong giỏ hàng sau khi tạo đơn hàng
                    _context.CartItems.RemoveRange(cartItems);
                    await _context.SaveChangesAsync();

                    // Commit transaction
                    await transaction.CommitAsync();

                    return new OkObjectResult(new { Message = "Đơn hàng đã được tạo.", OrderId = order.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi tạo đơn hàng cho người dùng có ID {UserId}", userId);
                    // Rollback transaction in case of error
                    await transaction.RollbackAsync();
                    return new StatusCodeResult(500);
                }
            }
        }

        public async Task<IActionResult> CreateGuestOrder(HttpContext httpContext)
        {
            // Sử dụng execution strategy của DbContext
            var executionStrategy = _context.Database.CreateExecutionStrategy();
            return await executionStrategy.ExecuteAsync(
                async () => await CreateGuestOrderInternal(httpContext));
        }

        private async Task<IActionResult> CreateGuestOrderInternal(HttpContext httpContext)
        {
            // Bắt đầu transaction
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Lấy thông tin từ session
                var previewDataJson = httpContext.Session.GetString("PreviewOrderData");
                if (string.IsNullOrEmpty(previewDataJson))
                {
                    return new BadRequestObjectResult("Không có dữ liệu đơn hàng tạm thời. Vui lòng thực hiện lại bước Preview.");
                }

                var previewData = JsonConvert.DeserializeObject<dynamic>(previewDataJson);

                // Kiểm tra thông tin bắt buộc từ preview
                var shippingAddress = (string)previewData.ShippingAddress;
                var phoneNumber = (string)previewData.PhoneNumber;
                var email = (string)previewData.Email;
                if (string.IsNullOrWhiteSpace(shippingAddress))
                {
                    return new BadRequestObjectResult("Địa chỉ giao hàng không được để trống.");
                }

                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    return new BadRequestObjectResult("Số điện thoại không được để trống.");
                }

                // Lấy giỏ hàng từ session
                var cartItemsJson = httpContext.Session.GetString("CartItems");
                if (string.IsNullOrEmpty(cartItemsJson))
                {
                    return new BadRequestObjectResult("Giỏ hàng trống. Vui lòng thêm sản phẩm.");
                }

                var cartItems = JsonConvert.DeserializeObject<List<CartItem>>(cartItemsJson);
                if (cartItems == null || !cartItems.Any())
                {
                    return new BadRequestObjectResult("Giỏ hàng trống. Vui lòng thêm sản phẩm.");
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

                // Lấy thông tin sản phẩm
                var productIds = cartItems.Select(ci => ci.ProductId).Distinct().ToList();
                var products = await _context.Products
    .Where(p => productIds.Contains(p.Id))
    .ToDictionaryAsync(p => p.Id, p => p.Price == 0 ? 0m : p.Price);

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
                httpContext.Session.Remove("CartItems");
                httpContext.Session.Remove("PreviewOrderData");

                // Commit transaction
                await transaction.CommitAsync();

                return new OkObjectResult(new
                {
                    Message = "Đơn hàng đã được tạo thành công.",
                    OrderId = order.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo đơn hàng cho khách vãng lai");
                // Rollback transaction
                await transaction.RollbackAsync();
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> CancelOrder(int orderId, string cancelReason)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    return new NotFoundObjectResult("Không tìm thấy đơn hàng.");
                }

                var result = await _stateContext.Cancel(order, cancelReason);
                if (result)
                {
                    return new OkObjectResult("Đơn hàng đã được hủy thành công.");
                }
                else
                {
                    return new BadRequestObjectResult("Không thể hủy đơn hàng trong trạng thái hiện tại.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi hủy đơn hàng ID: {orderId}");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> UpdateStatus(int orderId, string status, string paymentStatus, DateTime? deliveryDate)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    return new NotFoundObjectResult("Không tìm thấy đơn hàng.");
                }

                bool success = false;

                // Xử lý cập nhật trạng thái theo request
                switch (status)
                {
                    case "Chờ Lấy Hàng":
                        success = await _stateContext.ConfirmOrder(order);
                        break;
                    case "Đang Giao Hàng":
                        success = await _stateContext.Ship(order);
                        break;
                    case "Đã Giao":
                        success = await _stateContext.Deliver(order);
                        break;
                    case "Đã Hủy":
                        success = await _stateContext.Cancel(order, "Đơn hàng bị hủy bởi admin");
                        break;
                    default:
                        return new BadRequestObjectResult("Trạng thái không hợp lệ.");
                }

                if (success)
                {
                    // Cập nhật các thông tin khác
                    order.PaymentStatus = paymentStatus;
                    if (deliveryDate.HasValue)
                    {
                        order.EstimatedDeliveryDate = deliveryDate;
                    }
                    await _context.SaveChangesAsync();

                    return new OkObjectResult("Trạng thái đơn hàng đã được cập nhật.");
                }
                else
                {
                    return new BadRequestObjectResult("Không thể cập nhật trạng thái đơn hàng.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi cập nhật trạng thái đơn hàng ID: {orderId}");
                return new StatusCodeResult(500);
            }
        }
    }
}