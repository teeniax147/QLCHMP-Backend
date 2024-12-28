using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyCuaHangMyPham.Data;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyCuaHangMyPham.Controllers
{
    [Route("api/reports")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;
        private readonly ExportService _exportService;

        public ReportController(QuanLyCuaHangMyPhamContext context, ExportService exportService)
        {
            _context = context;
            _exportService = exportService;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("revenue/from-to")]
        public async Task<IActionResult> GetRevenueFromToDate(DateTime startDate, DateTime endDate, string format = "json")
        {
            var revenueData = await _context.Orders
                .Where(o => o.OrderDate.HasValue && o.OrderDate.Value >= startDate && o.OrderDate.Value <= endDate)
                .GroupBy(o => o.OrderDate.Value.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalRevenue = g.Sum(o => o.OrderDetails.Sum(od => (od.Quantity ?? 0) * (od.UnitPrice ?? 0))),
                    TotalOrders = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            if (format.ToLower() == "pdf")
            {
                var pdfBytes = _exportService.ExportToPdf("Báo cáo doanh thu", revenueData);
                return File(pdfBytes, "application/pdf", "BaoCaoDoanhThu.pdf");
            }
            else if (format.ToLower() == "excel")
            {
                var excelBytes = _exportService.ExportToExcel("Báo cáo doanh thu", revenueData);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "BaoCaoDoanhThu.xlsx");
            }

            return Ok(new
            {
                StartDate = startDate,
                EndDate = endDate,
                RevenueData = revenueData
            });
        }

        // 2. Thống kê doanh thu theo thương hiệu trong khoảng thời gian
        [HttpGet("revenue/brands/from-to")]
        public async Task<IActionResult> GetRevenueByBrandFromToDate(DateTime startDate, DateTime endDate)
        {
            var brandRevenue = await _context.Brands
                .Select(b => new
                {
                    BrandName = b.Name,
                    TotalRevenue = b.Products.Sum(p => p.OrderDetails
                        .Where(od => od.Order.OrderDate.HasValue && od.Order.OrderDate.Value >= startDate && od.Order.OrderDate.Value <= endDate)
                        .Sum(od => (od.Quantity ?? 0) * (od.UnitPrice ?? 0)))
                })
                .OrderByDescending(br => br.TotalRevenue)
                .ToListAsync();

            return Ok(new
            {
                StartDate = startDate,
                EndDate = endDate,
                BrandRevenue = brandRevenue
            });
        }

        // 3. Thống kê sản phẩm bán chạy trong khoảng thời gian
        [HttpGet("products/top-selling/from-to")]
        public async Task<IActionResult> GetTopSellingProductsFromToDate(DateTime startDate, DateTime endDate, int top = 5)
        {
            var topProducts = await _context.Products
                .Select(p => new
                {
                    ProductName = p.Name,
                    TotalSold = p.OrderDetails
                        .Where(od => od.Order.OrderDate.HasValue && od.Order.OrderDate.Value >= startDate && od.Order.OrderDate.Value <= endDate)
                        .Sum(od => od.Quantity ?? 0)
                })
                .OrderByDescending(tp => tp.TotalSold)
                .Take(top)
                .ToListAsync();

            return Ok(new
            {
                StartDate = startDate,
                EndDate = endDate,
                TopProducts = topProducts
            });
        }
        // 4. Thống kê khách hàng chi tiêu nhiều nhất trong khoảng thời gian
        [HttpGet("customers/top-spenders/from-to")]
        public async Task<IActionResult> GetTopSpendingCustomersFromToDate(DateTime startDate, DateTime endDate, int top = 5)
        {
            var topCustomers = await _context.Customers
                .Include(c => c.User) // Bao gồm thông tin ApplicationUser
                .Select(c => new
                {
                    CustomerName = $"{c.User.FirstName} {c.User.LastName}",
                    TotalSpending = c.Orders
                        .Where(o => o.OrderDate.HasValue && o.OrderDate.Value >= startDate && o.OrderDate.Value <= endDate)
                        .Sum(o => o.OrderDetails.Sum(od => (od.Quantity ?? 0) * (od.UnitPrice ?? 0)))
                })
                .OrderByDescending(tc => tc.TotalSpending)
                .Take(top)
                .ToListAsync();

            return Ok(new
            {
                StartDate = startDate,
                EndDate = endDate,
                TopCustomers = topCustomers
            });
        }
        // 5. Thống kê số lượng sản phẩm bán ra theo ngày trong khoảng thời gian
        [HttpGet("products/sales/from-to")]
        public async Task<IActionResult> GetProductSalesFromToDate(DateTime startDate, DateTime endDate)
        {
            var productSales = await _context.OrderDetails
                .Where(od => od.Order.OrderDate.HasValue && od.Order.OrderDate.Value >= startDate && od.Order.OrderDate.Value <= endDate)
                .GroupBy(od => od.Order.OrderDate.Value.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalProductsSold = g.Sum(od => od.Quantity ?? 0)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return Ok(new
            {
                StartDate = startDate,
                EndDate = endDate,
                ProductSales = productSales
            });
        }

        // 4. Thống kê sản phẩm gần hết hàng
        [HttpGet("products/low-stock")]
        public async Task<IActionResult> GetLowStockProducts(int threshold = 10)
        {
            var lowStockProducts = await _context.Products
                .Where(p => p.Inventories.Sum(i => i.QuantityInStock) <= threshold)
                .Select(p => new
                {
                    ProductName = p.Name,
                    Stock = p.Inventories.Sum(i => i.QuantityInStock)
                })
                .OrderBy(p => p.Stock)
                .ToListAsync();

            return Ok(lowStockProducts);
        }

        // 7. Thống kê lượt xem blog theo khoảng thời gian
        [HttpGet("blogs/view-count/from-to")]
        public async Task<IActionResult> GetBlogViewCountFromToDate(DateTime startDate, DateTime endDate)
        {
            var blogViews = await _context.BeautyBlogs
                .Where(b => b.CreatedAt.HasValue && b.CreatedAt.Value >= startDate && b.CreatedAt.Value <= endDate)
                .Select(b => new
                {
                    BlogTitle = b.Title,
                    ViewCount = b.ViewCount,
                    CreatedAt = b.CreatedAt
                })
                .OrderByDescending(b => b.ViewCount)
                .ToListAsync();

            return Ok(new
            {
                StartDate = startDate,
                EndDate = endDate,
                BlogViews = blogViews
            });
        }

        // Doanh thu theo danh mục trong khoảng thời gian
        [HttpGet("revenue/by-categories/from-to")]
        public async Task<IActionResult> GetRevenueByCategoriesFromToDate(DateTime startDate, DateTime endDate)
        {
            var categoryRevenue = await _context.Categories
                .Select(c => new
                {
                    CategoryName = c.Name,
                    TotalRevenue = c.Products
                        .SelectMany(p => p.OrderDetails)
                        .Where(od => od.Order.OrderDate.HasValue && od.Order.OrderDate.Value >= startDate && od.Order.OrderDate.Value <= endDate)
                        .Sum(od => (od.Quantity ?? 0) * (od.UnitPrice ?? 0))
                })
                .OrderByDescending(cr => cr.TotalRevenue)
                .ToListAsync();

            return Ok(new
            {
                StartDate = startDate,
                EndDate = endDate,
                CategoryRevenue = categoryRevenue
            });
        }


        // Tỉ lệ hoàn thành đơn hàng từ ngày đến ngày
        [HttpGet("orders/completion-rate/from-to")]
        public async Task<IActionResult> GetOrderCompletionRateFromToDate(DateTime startDate, DateTime endDate)
        {
            var orderCompletion = await _context.Orders
                .Where(o => o.OrderDate.HasValue && o.OrderDate.Value >= startDate && o.OrderDate.Value <= endDate)
                .GroupBy(o => o.OrderDate.Value.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalOrders = g.Count(),
                    CompletedOrders = g.Count(o => o.Status == "Completed"),
                    CompletionRate = (double)g.Count(o => o.Status == "Completed") / g.Count() * 100
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return Ok(new
            {
                StartDate = startDate,
                EndDate = endDate,
                OrderCompletionRate = orderCompletion
            });
        }




        // 12. // Thống kê số lượng sản phẩm yêu thích nhất
        [HttpGet("products/most-favorites")]
        public async Task<IActionResult> GetMostFavoriteProducts(int top = 5)
        {
            var mostFavoriteProducts = await _context.Products
                .OrderByDescending(p => p.FavoriteCount ?? 0) // Sắp xếp theo số lượng yêu thích giảm dần
                .Take(top) // Giới hạn số lượng sản phẩm trả về
                .Select(p => new
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    FavoriteCount = p.FavoriteCount ?? 0,
                    BrandName = p.Brand != null ? p.Brand.Name : "Unknown",
                    CategoryNames = p.Categories.Select(c => c.Name).ToList()
                })
                .ToListAsync();

            return Ok(new
            {
                TopProducts = mostFavoriteProducts,
                TotalProducts = mostFavoriteProducts.Count
            });
        }



        // 14. Thống Kê Số Lượng Bài Viết Blog Theo Ngày hoặc Theo Khoảng Ngày
        [HttpGet("blogs/posts-count")]
        public async Task<IActionResult> GetBlogPostCount([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            // Nếu không cung cấp khoảng thời gian, mặc định lấy toàn bộ dữ liệu
            var query = _context.BeautyBlogs.AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(b => b.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(b => b.CreatedAt <= endDate.Value);
            }

            var postCount = await query
                .GroupBy(b => b.CreatedAt.Value.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    PostCount = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return Ok(new
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalPosts = postCount.Sum(x => x.PostCount),
                DailyPostCounts = postCount
            });
        }



        // 16. Khách hàng chi tiêu nhiều nhất
        [Authorize(Roles = "Admin")]
        [HttpGet("customers/top-spenders")]
        public async Task<IActionResult> GetTopSpendingCustomers(
    [FromQuery] DateTime? startDate = null,
    [FromQuery] DateTime? endDate = null,
    int top = 5)
        {
            // Truy vấn lấy khách hàng cùng các đơn hàng trong khoảng thời gian
            var query = _context.Customers
                .Include(c => c.User) // Bao gồm thông tin người dùng từ ApplicationUser
                .Select(c => new
                {
                    CustomerName = $"{c.User.FirstName} {c.User.LastName}",
                    TotalSpending = c.Orders
                        .Where(o => (!startDate.HasValue || o.OrderDate >= startDate) &&
                                    (!endDate.HasValue || o.OrderDate <= endDate))
                        .Sum(o => o.OrderDetails.Sum(od => (od.Quantity ?? 0) * (od.UnitPrice ?? 0)))
                });

            // Lấy danh sách khách hàng có tổng chi tiêu cao nhất
            var topCustomers = await query
                .OrderByDescending(c => c.TotalSpending)
                .Take(top)
                .ToListAsync();

            return Ok(new
            {
                StartDate = startDate,
                EndDate = endDate,
                TopCustomers = topCustomers
            });
        }
    }
}
