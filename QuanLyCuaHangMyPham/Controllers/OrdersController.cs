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
using Microsoft.Data.SqlClient;

namespace QuanLyCuaHangMyPham.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;
        private readonly ILogger<CartsController> _logger;
        private readonly IMemoryCache _cache;  // Khai báo _cache
        public OrdersController(QuanLyCuaHangMyPhamContext context, IMemoryCache cache, ILogger<CartsController> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
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
        [HttpGet("filter-by-status")]
        public async Task<IActionResult> FilterOrdersByStatus(string status)
        {
            // Kiểm tra giá trị status hợp lệ
            if (!new[] { "Chờ Xác Nhận", "Chờ Lấy Hàng", "Đang Giao Hàng", "Đã Giao", "Đã Hủy" }.Contains(status))
            {
                return BadRequest("Trạng thái không hợp lệ.");
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
                CustomerName = $"{o.Customer.User.FirstName} {o.Customer.User.LastName}",
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
        [Authorize(Roles = "Admin")]
        [HttpGet("by-date")]
        public async Task<IActionResult> GetOrdersByDate(DateTime date)
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

            return Ok(ordersOnDate);
        }
        //dùng để sử dụng cho reportcontroller
        [Authorize(Roles = "Admin")]
        [HttpGet("order-details/{orderId}")]
        public async Task<IActionResult> GetOrderDetailsById(int orderId)
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
                return NotFound("Không tìm thấy đơn hàng.");
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

            return Ok(new
            {
                OrderInfo = orderInfo,
                OrderDetails = orderDetails
            });
        }
        //dùng để sử dụng cho reportcontroller

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
        [HttpGet("search")]
        public async Task<IActionResult> SearchOrders(
    string? searchTerm,
    int page = 1,
    int pageSize = 10,
    string? status = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 10;

            // Bắt đầu với truy vấn IQueryable
            var query = _context.Orders
                .Include(o => o.Customer)
                    .ThenInclude(c => c.User)
                .AsQueryable();

            // Áp dụng tìm kiếm nếu có searchTerm
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim().ToLower();
                query = query.Where(o =>
                    // Tìm theo tên khách hàng (kết hợp FirstName và LastName)
                    (o.Customer.User.FirstName + " " + o.Customer.User.LastName).ToLower().Contains(searchTerm) ||
                    // Tìm theo PhoneNumber
                    (o.PhoneNumber != null && o.PhoneNumber.ToLower().Contains(searchTerm)) ||
                    // Tìm theo Email
                    (o.Email != null && o.Email.ToLower().Contains(searchTerm))
                );
            }

            // Lọc theo trạng thái nếu được chỉ định
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.Status != null && o.Status.ToLower() == status.ToLower());
            }

            // Đếm tổng số kết quả để tính phân trang
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Lấy dữ liệu theo trang
            var orders = await query
                .OrderByDescending(o => o.OrderDate) // Sắp xếp mới nhất trước
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrderViewModel
                {
                    Id = o.Id,
                    OrderDate = o.OrderDate,
                    Status = o.Status,
                    PaymentStatus = o.PaymentStatus,
                    TotalAmount = o.TotalAmount,
                    CustomerName = o.Customer.User.FirstName + " " + o.Customer.User.LastName,
                    PhoneNumber = o.PhoneNumber,
                    Email = o.Email,
                    ShippingAddress = o.ShippingAddress
                })
                .ToListAsync();

            // Trả về kết quả kèm thông tin phân trang
            return Ok(new
            {
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = page,
                PageSize = pageSize,
                Orders = orders
            });
        }

        // ViewModel để trả về kết quả tìm kiếm
        public class OrderViewModel
        {
            public int Id { get; set; }
            public DateTime? OrderDate { get; set; }
            public string Status { get; set; }
            public string PaymentStatus { get; set; }
            public decimal? TotalAmount { get; set; }
            public string CustomerName { get; set; }
            public string PhoneNumber { get; set; }
            public string Email { get; set; }
            public string ShippingAddress { get; set; }
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
