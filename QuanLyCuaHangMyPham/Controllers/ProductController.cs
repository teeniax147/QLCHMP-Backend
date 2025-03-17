using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using iText.Commons.Actions.Data;
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
    public class ProductsController : ControllerBase
    {
        private readonly QuanLyCuaHangMyPhamContext _context;

        public ProductsController(QuanLyCuaHangMyPhamContext context)
        {
            _context = context;
        }
        [HttpGet("for-java")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
        {
            var products = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Categories)
                .Include(p => p.Inventories)
                .Include(p => p.ProductFeedbacks)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    OriginalPrice = p.OriginalPrice,
                    Description = p.Description,
                    ImageUrl = p.ImageUrl,
                    Categories = p.Categories.Select(c => string.IsNullOrEmpty(c.Name) ? "Không xác định" : c.Name).ToList(),
                    BrandName = p.Brand != null ? p.Brand.Name : null,
                    ShockPrice = p.ShockPrice,
                    CurrentStock = p.GetCurrentStock()
                })
                .ToListAsync();

            return Ok(products);
        }
        [HttpGet("danh-sach")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetAllProducts(int pageNumber = 1, int pageSize = 10)
        {
            if (pageNumber <= 0 || pageSize <= 0)
            {
                return BadRequest("Số trang và số sản phẩm mỗi trang phải lớn hơn 0.");
            }

            var totalProducts = await _context.Products.CountAsync();

            var products = await _context.Products

                .Include(p => p.Brand) // Lấy thông tin Brand
                .Include(p => p.Categories) // Lấy danh mục của sản phẩm
                .Include(p => p.Inventories) // Lấy thông tin tồn kho
                .Include(p => p.ProductFeedbacks) // Lấy đánh giá sản phẩm
                .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    OriginalPrice = p.OriginalPrice,
                    Description = p.Description,
                    ImageUrl = p.ImageUrl,
                    BrandName = p.Brand != null ? p.Brand.Name : "Không có thương hiệu",
                    Categories = p.Categories.Select(c => string.IsNullOrEmpty(c.Name) ? "Không xác định" : c.Name).ToList(), // Sửa lỗi null ở đây
                    CurrentStock = p.Inventories.Sum(i => i.QuantityInStock),
                    ShockPrice = p.ShockPrice,
                    AverageRating = p.ProductFeedbacks.Any() ? (double?)p.ProductFeedbacks.Average(f => f.Rating) : null,
                    ReviewCount = p.ProductFeedbacks.Count





                })
                .ToListAsync();

            return Ok(new
            {
                DanhSachSanPham = products,
                TongSoSanPham = totalProducts,
                SoTrang = pageNumber,
                SoSanPhamMoiTrang = pageSize
            });
        }
        [HttpGet("loc")]
        public async Task<ActionResult<IEnumerable<ProductFilterDto>>> FilterProducts([FromQuery] FilterProductsRequest request)
        {
            if (request.PageNumber <= 0 || request.PageSize <= 0)
            {
                return BadRequest("Số trang và số sản phẩm mỗi trang phải lớn hơn 0.");
            }

            // Truy vấn cơ bản
            var products = _context.Products
                .Where(p => p.IsActive)
                .Include(p => p.Brand)
                .Include(p => p.Categories)
                .Include(p => p.Inventories)
                .Include(p => p.ProductFeedbacks)
                .Include(p => p.Promotions)
                .AsQueryable();

            // Áp dụng bộ lọc
            if (request.MinPrice.HasValue)
            {
                products = products.Where(p => p.Price >= request.MinPrice.Value);
            }
            if (request.MaxPrice.HasValue)
            {
                products = products.Where(p => p.Price <= request.MaxPrice.Value);
            }
            if (request.MinStock.HasValue)
            {
                products = products.Where(p => p.Inventories.Sum(i => i.QuantityInStock) >= request.MinStock.Value);
            }
            if (request.MaxStock.HasValue)
            {
                products = products.Where(p => p.Inventories.Sum(i => i.QuantityInStock) <= request.MaxStock.Value);
            }
            if (request.IsOnSale.HasValue)
            {
                products = products.Where(p => p.Promotions.Any(pr => pr.StartDate <= DateTime.Now && pr.EndDate >= DateTime.Now) == request.IsOnSale.Value);
            }
            if (request.BrandId.HasValue)
            {
                products = products.Where(p => p.BrandId == request.BrandId.Value);
            }

            // Truy vấn toàn bộ sản phẩm trong cơ sở dữ liệu
            var allProducts = _context.Products.Where(p => p.IsActive).ToList();

            // Tính MaxPrice và MinPrice từ tất cả sản phẩm
            var maxPrice = allProducts.Max(p => p.Price);
            var minPrice = allProducts.Min(p => p.Price);

            // Tính giá trị trung bình của tất cả các sản phẩm
            decimal avgPrice = allProducts.Average(p => p.Price);

            // Lấy các sản phẩm có giá lớn hơn giá trị trung bình và sắp xếp theo độ chênh lệch với giá trị trung bình
            var productsAboveAvg = allProducts
                .Where(p => p.Price > avgPrice)
                .OrderBy(p => Math.Abs(p.Price - avgPrice))  // Sắp xếp theo sự chênh lệch với giá trị trung bình
                .Take(5   )  // Lấy 5 sản phẩm trên giá trị trung bình
                .ToList();

            // Lấy các sản phẩm có giá thấp hơn giá trị trung bình và sắp xếp theo độ chênh lệch với giá trị trung bình
            var productsBelowAvg = allProducts
                .Where(p => p.Price < avgPrice)
                .OrderBy(p => Math.Abs(p.Price - avgPrice))  // Sắp xếp theo sự chênh lệch với giá trị trung bình
                .Take(5)  // Lấy 5 sản phẩm dưới giá trị trung bình
                .ToList();

            // Kết hợp các sản phẩm có giá lớn hơn và nhỏ hơn giá trị trung bình
            var closestProducts = productsAboveAvg.Concat(productsBelowAvg).ToList();

            // Nếu không có sản phẩm nào thỏa mãn bộ lọc, dùng closestProducts
            var productsToReturn = products.Any() ? products : closestProducts.AsQueryable();

            // Áp dụng bộ lọc giá nếu có từ request
            if (request.MinPrice.HasValue && request.MaxPrice.HasValue)
            {
                productsToReturn = productsToReturn.Where(p => p.Price >= request.MinPrice.Value && p.Price <= request.MaxPrice.Value);
            }

            // Sắp xếp
            if (!string.IsNullOrEmpty(request.SortByPrice))
            {
                productsToReturn = request.SortByPrice.ToLower() switch
                {
                    "asc" => productsToReturn.OrderBy(p => p.Price),
                    "desc" => productsToReturn.OrderByDescending(p => p.Price),
                    "avgasc" => productsToReturn.OrderBy(p => avgPrice),
                    "avgdesc" => productsToReturn.OrderByDescending(p => avgPrice),
                    _ => productsToReturn
                };
            }

            // Tổng số sản phẩm sau khi lọc
            var totalFilteredProducts = await productsToReturn.CountAsync();

            // Phân trang và chuyển sang DTO
            var pagedFilteredProducts = await productsToReturn
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new ProductFilterDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    OriginalPrice = p.OriginalPrice,
                    Description = p.Description,
                    ImageUrl = p.ImageUrl,
                    BrandName = p.Brand != null ? p.Brand.Name : "Không có thương hiệu",
                    Categories = p.Categories.Select(c => c.Name ?? "Không xác định").ToList(),
                    CurrentStock = p.Inventories.Sum(i => i.QuantityInStock),
                    ShockPrice = p.ShockPrice,
                    Promotions = p.Promotions.Select(pr => pr.Name).ToList()
                })
                .ToListAsync();

            return Ok(new
            {
                DanhSachSanPham = pagedFilteredProducts,
                TongSoSanPham = totalFilteredProducts,
                SoTrang = request.PageNumber,
                SoSanPhamMoiTrang = request.PageSize,
                MaxPrice = maxPrice,
                MinPrice = minPrice,
                GiaTrungBinh = avgPrice // Thêm giá trị trung bình vào kết quả
            });
        }


        // GET: api/san-pham/theo-danh-muc/{categoryId}
        [HttpGet("theo-danh-muc/{categoryId}")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductsByCategory(int categoryId, int pageNumber = 1, int pageSize = 10)
        {
            if (pageNumber <= 0 || pageSize <= 0)
            {
                return BadRequest("Số trang và số sản phẩm mỗi trang phải lớn hơn 0.");
            }

            // Lọc sản phẩm theo categoryId
            var products = _context.Products
                .Include(p => p.Brand) // Bao gồm thông tin thương hiệu
                .Include(p => p.Inventories) // Bao gồm thông tin tồn kho
                .Include(p => p.Promotions) // Bao gồm khuyến mãi
                .Where(p => p.Categories.Any(c => c.Id == categoryId)) // Lọc theo danh mục
                .AsQueryable();

            var totalProducts = await products.CountAsync();

            var pagedProducts = await products
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .Select(p => new
    {
        Id = p.Id,
        Name = p.Name,
        Price = p.Price,
        OriginalPrice = p.OriginalPrice,
        BrandName = p.Brand != null ? p.Brand.Name : "Không có thương hiệu", // Xử lý null cho thương hiệu
        CurrentStock = p.Inventories != null && p.Inventories.Any()
            ? p.Inventories.Sum(i => i.QuantityInStock) // Tổng tồn kho
            : 0, // Fallback nếu không có tồn kho
        ImageUrl = string.IsNullOrEmpty(p.ImageUrl)
            ? "https://via.placeholder.com/150" // Hình mặc định nếu không có URL
            : p.ImageUrl,
        Promotions = p.Promotions.Select(pr => pr.Name).ToList(), // Danh sách khuyến mãi (nếu cần)
    })
    .ToListAsync();

            return Ok(new
            {
                DanhSachSanPham = pagedProducts, // Danh sách sản phẩm đã xử lý
                TongSoSanPham = totalProducts, // Tổng số sản phẩm
                SoTrang = pageNumber, // Trang hiện tại
                SoSanPhamMoiTrang = pageSize // Số sản phẩm mỗi trang
            });
        }

        [HttpGet("chi-tiet/{id}")]
        public async Task<ActionResult<ProductDetailDto>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Brand) // Thương hiệu
                .Include(p => p.Categories) // Danh mục sản phẩm
                .Include(p => p.Inventories) // Tồn kho
                .Include(p => p.ProductFeedbacks) // Lấy đánh giá sản phẩm
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound("Không tìm thấy sản phẩm.");
            }

            // Chuyển đổi sang DTO
            var productDto = new ProductDetailDto
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                OriginalPrice = product.OriginalPrice,
                Description = product.Description,
                ImageUrl = product.ImageUrl,
                BrandName = product.Brand?.Name,
                Categories = product.Categories.Select(c => c.Name ?? "Không xác định").ToList(),
                CurrentStock = product.Inventories.Sum(i => i.QuantityInStock),
                ShockPrice = product.ShockPrice,

            };

            return Ok(productDto);
        }


        [Authorize(Roles = "Admin")]
        [HttpPost("them-moi")]
        public async Task<ActionResult<Product>> PostProduct([FromForm] CreateProductRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Xử lý upload ảnh sản phẩm
            string imagePath = null;
            if (request.ImageFile != null && request.ImageFile.Length > 0)
            {
                var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(request.ImageFile.FileName);
                var filePath = Path.Combine(uploadDir, uniqueFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await request.ImageFile.CopyToAsync(stream);
                }
                imagePath = $"/uploads/products/{uniqueFileName}";
            }

            try
            {
                // Tìm danh sách thương hiệu từ BrandIds - TÁCH RIÊNG TRUY VẤN
                List<Brand> brands = new List<Brand>();
                foreach (var brandId in request.BrandIds)
                {
                    var brand = await _context.Brands.FindAsync(brandId);
                    if (brand != null)
                    {
                        brands.Add(brand);
                    }
                }

                if (!brands.Any())
                {
                    return BadRequest("Danh sách thương hiệu không hợp lệ.");
                }

                // Tạo mới đối tượng Product
                var product = new Product
                {
                    Name = request.Name,
                    Price = request.Price,
                    OriginalPrice = request.OriginalPrice,
                    Description = request.Description,
                    ImageUrl = imagePath,
                    Brand = brands.FirstOrDefault()
                };

                // TÁCH RIÊNG TRUY VẤN CHO CATEGORIES
                if (request.Categories != null && request.Categories.Any())
                {
                    product.Categories = new List<Category>();
                    foreach (var categoryId in request.Categories)
                    {
                        var category = await _context.Categories.FindAsync(categoryId);
                        if (category != null)
                        {
                            product.Categories.Add(category);
                        }
                    }

                    if (!product.Categories.Any())
                    {
                        return BadRequest("Danh sách danh mục không hợp lệ.");
                    }
                }

                // Thêm sản phẩm mới
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // LƯU CHANGES TRƯỚC KHI THỰC HIỆN THAO TÁC TIẾP THEO

                // Tạo kho hàng với số lượng ban đầu = 0
                var inventory = new Inventory
                {
                    ProductId = product.Id,
                    QuantityInStock = 0,
                    WarehouseLocation = "Chưa xác định",
                    LastUpdated = DateTime.UtcNow
                };

                _context.Inventories.Add(inventory);
                await _context.SaveChangesAsync();

                // Chuẩn bị dữ liệu trả về
                var response = new
                {
                    Id = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    OriginalPrice = product.OriginalPrice,
                    Description = product.Description,
                    ImageUrl = product.ImageUrl,
                    Brands = brands.Select(b => new { b.Id, b.Name }).ToList(),
                    Categories = product.Categories.Select(c => new { c.Id, c.Name }).ToList()
                };

                return CreatedAtAction("GetProduct", new { id = product.Id }, response);
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Không thể lưu sản phẩm mới.", error = ex.Message });
            }
        }





        // PUT: api/san-pham/cap-nhat/{id}
        [Authorize(Roles = "Admin")]
        [HttpPut("cap-nhat/{id}")]
        public async Task<IActionResult> PutProduct(int id, [FromForm] UpdateProductRequest request)
        {
            if (id != request.Id)
            {
                return BadRequest("ID sản phẩm không khớp.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Tìm sản phẩm trong cơ sở dữ liệu
            var product = await _context.Products
                .Include(p => p.Brand) // Thương hiệu
                .Include(p => p.Categories) // Danh mục
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound("Không tìm thấy sản phẩm.");
            }

            // Xử lý ảnh tải lên
            string? imagePath = product.ImageUrl; // Giữ ảnh cũ nếu không có ảnh mới

            if (request.ImageFile != null && request.ImageFile.Length > 0)
            {
                var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(request.ImageFile.FileName);
                var filePath = Path.Combine(uploadDir, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await request.ImageFile.CopyToAsync(stream);
                }

                // Xóa ảnh cũ nếu có
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", product.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                imagePath = $"/uploads/products/{uniqueFileName}";
            }

            // Cập nhật thông tin sản phẩm
            product.Name = request.Name;
            product.Price = request.Price;
            product.OriginalPrice = request.OriginalPrice;
            product.Description = request.Description;
            product.ImageUrl = imagePath;

            // Cập nhật danh sách thương hiệu
            if (request.BrandIds != null && request.BrandIds.Any())
            {
                var brands = await _context.Brands
                    .Where(b => request.BrandIds.Contains(b.Id))
                    .ToListAsync();

                if (!brands.Any())
                {
                    return BadRequest("Danh sách thương hiệu không hợp lệ.");
                }

                product.Brand = brands.FirstOrDefault(); // Lấy thương hiệu đầu tiên làm đại diện
            }

            // Cập nhật danh sách danh mục
            if (request.Categories != null && request.Categories.Any())
            {
                var categories = await _context.Categories
                    .Where(c => request.Categories.Contains(c.Id))
                    .ToListAsync();

                if (!categories.Any())
                {
                    return BadRequest("Danh sách danh mục không hợp lệ.");
                }

                product.Categories = categories;
            }

            try
            {
                // Lưu thay đổi vào cơ sở dữ liệu
                await _context.SaveChangesAsync();

                // Chuẩn bị dữ liệu trả về
                var response = new
                {
                    Id = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    OriginalPrice = product.OriginalPrice,
                    Description = product.Description,
                    ImageUrl = product.ImageUrl,
                    Brands = new { product.Brand?.Id, product.Brand?.Name },
                    Categories = product.Categories.Select(c => new { c.Id, c.Name }).ToList()
                };

                return Ok(response);
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Không thể cập nhật sản phẩm.", error = ex.Message });
            }
        }



        // DELETE: api/san-pham/xoa/{id}
        [Authorize(Roles = "Admin")]
        [HttpDelete("xoa/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound("Không tìm thấy sản phẩm để xóa.");
            }

            try
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                return Ok("Xóa sản phẩm thành công.");
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Không thể xóa sản phẩm.", error = ex.Message });
            }
        }

        // API Lấy sản phẩm bán chạy nhất
        [HttpGet("ban-chay-nhat")]
        public async Task<ActionResult<IEnumerable<Product>>> GetBestSellingProducts([FromQuery] BestSellingProductsRequest request)
        {
            var bestSellers = await _context.Products
                .Include(p => p.OrderDetails)
                .OrderByDescending(p => p.OrderDetails.Sum(od => od.Quantity))
                .Take(request.Top)
                .ToListAsync();

            return Ok(bestSellers);
        }

        // API Lấy sản phẩm tương tự dựa trên danh mục
        [HttpGet("tuong-tu/{productId}")]
        public async Task<ActionResult<IEnumerable<Product>>> GetSimilarProducts(int productId, [FromQuery] SimilarProductsRequest request)
        {
            var product = await _context.Products
                .Include(p => p.Categories)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null || product.Categories == null || !product.Categories.Any())
            {
                return NotFound("Không tìm thấy sản phẩm hoặc sản phẩm không có danh mục.");
            }

            var categoryIds = product.Categories.Select(c => c.Id).ToList();
            var similarProducts = await _context.Products
                .Include(p => p.Categories)
                .Where(p => p.Id != productId && p.Categories.Any(c => categoryIds.Contains(c.Id)))
                .Take(request.Top)
                .ToListAsync();

            return Ok(similarProducts);
        }
        // API Tìm kiếm sản phẩm theo từ khóa
        [HttpGet("tim-kiem")]
        public async Task<ActionResult<IEnumerable<object>>> SearchProducts([FromQuery] SearchProductsRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Keyword))
            {
                return BadRequest("Vui lòng nhập từ khóa tìm kiếm.");
            }

            // Tìm sản phẩm dựa trên từ khóa
            var query = _context.Products

                .Include(p => p.Brand)
                .Include(p => p.Categories)
                .Include(p => p.Inventories)
                .AsQueryable();
            // Kiểm tra quyền
            if (!User.IsInRole("Admin"))
            {
                // Khách hàng chỉ thấy sản phẩm kích hoạt
                query = query.Where(p => p.IsActive);
            }
            // Áp dụng tìm kiếm theo tên hoặc mô tả
            query = query.Where(p =>
                p.Name.Contains(request.Keyword) ||
                (p.Description != null && p.Description.Contains(request.Keyword))
            );

            // Phân trang
            var totalProducts = await query.CountAsync();
            var products = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            // Trả về kết quả cho khách hàng hoặc quản lý
            if (User.IsInRole("Admin"))
            {
                // Phản hồi chi tiết hơn cho quản lý
                var adminResponse = products.Select(p => new
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    OriginalPrice = p.OriginalPrice,
                    Description = p.Description,
                    ImageUrl = p.ImageUrl,
                    BrandName = p.Brand?.Name,
                    Categories = p.Categories.Select(c => c.Name).ToList(),
                    CurrentStock = p.Inventories.Sum(i => i.QuantityInStock)
                });

                return Ok(new
                {
                    TotalProducts = totalProducts,
                    Products = adminResponse,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                });
            }
            else
            {
                // Phản hồi đơn giản hơn cho khách hàng
                var customerResponse = products.Select(p => new
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Description = p.Description,
                    ImageUrl = p.ImageUrl,
                    BrandName = p.Brand?.Name
                });

                return Ok(new
                {
                    TotalProducts = totalProducts,
                    Products = customerResponse,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                });
            }
        }
        [Authorize(Roles = "Admin")]
        [HttpPut("active/{id}")]
        public async Task<IActionResult> ActivateProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound("Không tìm thấy sản phẩm.");
            }

            if (product.IsActive)
            {
                return BadRequest("Sản phẩm đã được kích hoạt.");
            }

            product.IsActive = true;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Sản phẩm đã được kích hoạt thành công.", ProductId = product.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Đã xảy ra lỗi khi kích hoạt sản phẩm.", Error = ex.Message });
            }
        }
        [Authorize(Roles = "Admin")]
        [HttpPut("unactive/{id}")]
        public async Task<IActionResult> DeactivateProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound("Không tìm thấy sản phẩm.");
            }

            if (!product.IsActive)
            {
                return BadRequest("Sản phẩm đã ở trạng thái không kích hoạt.");
            }

            product.IsActive = false;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Sản phẩm đã được hủy kích hoạt thành công.", ProductId = product.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Đã xảy ra lỗi khi hủy kích hoạt sản phẩm.", Error = ex.Message });
            }
        }


        // Kiểm tra xem sản phẩm có tồn tại không
        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
        public class ProductDto
        {
            public int Id { get; set; } // ID của sản phẩm
            public string Name { get; set; } = null!; // Tên sản phẩm
            public decimal Price { get; set; } // Giá hiện tại
            public decimal OriginalPrice { get; set; } // Giá gốc
            public string? Description { get; set; } // Mô tả sản phẩm
            public string? ImageUrl { get; set; } // URL hình ảnh sản phẩm
            public string? BrandName { get; set; } // Tên thương hiệu
            public List<string>? Categories { get; set; } // Danh sách tên danh mục sản phẩm
            public int CurrentStock { get; set; } // Tổng số lượng tồn kho
            public decimal? ShockPrice { get; set; } // Giá khuyến mãi (nếu có)
            public double? AverageRating { get; set; } // Đánh giá trung bình (nếu có)
            public int ReviewCount { get; set; } // Tổng số lượt đánh giá
        }
        // Request cho API thêm mới sản phẩm
        public class CreateProductRequest
        {
            [Required(ErrorMessage = "Tên sản phẩm là bắt buộc.")]
            public string Name { get; set; } = null!;

            [Required(ErrorMessage = "Giá sản phẩm là bắt buộc.")]
            [Range(0, double.MaxValue, ErrorMessage = "Giá sản phẩm phải là số dương.")]
            public decimal Price { get; set; }

            [Range(0, double.MaxValue, ErrorMessage = "Giá gốc phải là số dương.")]
            public decimal OriginalPrice { get; set; }

            public string? Description { get; set; }

            public IFormFile? ImageFile { get; set; } // Dùng để upload file ảnh

            [Required(ErrorMessage = "Danh sách thương hiệu là bắt buộc.")]
            public List<int> BrandIds { get; set; } = new List<int>(); // Danh sách ID thương hiệu

            public List<int>? Categories { get; set; } // Danh sách ID danh mục
        }

        public class UpdateProductRequest
        {
            [Required(ErrorMessage = "ID sản phẩm là bắt buộc.")]
            public int Id { get; set; }

            [Required(ErrorMessage = "Tên sản phẩm là bắt buộc.")]
            public string Name { get; set; } = null!;

            [Required(ErrorMessage = "Giá sản phẩm là bắt buộc.")]
            [Range(0, double.MaxValue, ErrorMessage = "Giá sản phẩm phải là số dương.")]
            public decimal Price { get; set; }

            [Range(0, double.MaxValue, ErrorMessage = "Giá gốc phải là số dương.")]
            public decimal OriginalPrice { get; set; }

            public string? Description { get; set; }

            public IFormFile? ImageFile { get; set; } // Thay đổi để hỗ trợ upload file ảnh

            [Required(ErrorMessage = "Danh sách thương hiệu là bắt buộc.")]
            public List<int> BrandIds { get; set; } = new List<int>(); // Danh sách ID thương hiệu

            public List<int>? Categories { get; set; } // Danh sách ID danh mục
        }

        public class ProductFilterDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = null!;
            public decimal Price { get; set; }
            public decimal OriginalPrice { get; set; }
            public string? Description { get; set; }
            public string? ImageUrl { get; set; }
            public string? BrandName { get; set; }
            public List<string>? Categories { get; set; }
            public int CurrentStock { get; set; }
            public decimal? ShockPrice { get; set; }
            public List<string>? Promotions { get; set; }
        }
        // Request cho API lọc sản phẩm
        public class FilterProductsRequest
        {
            [Range(1, int.MaxValue, ErrorMessage = "Số trang phải lớn hơn hoặc bằng 1.")]
            public int PageNumber { get; set; } = 1;

            [Range(1, int.MaxValue, ErrorMessage = "Số sản phẩm mỗi trang phải lớn hơn hoặc bằng 1.")]
            public int PageSize { get; set; } = 10;

            [Range(0, double.MaxValue, ErrorMessage = "Giá tối thiểu phải là số không âm.")]
            public decimal? MinPrice { get; set; }

            [Range(0, double.MaxValue, ErrorMessage = "Giá tối đa phải là số không âm.")]
            public decimal? MaxPrice { get; set; }

            [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho tối thiểu phải lớn hơn hoặc bằng 0.")]
            public int? MinStock { get; set; }

            [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho tối đa phải lớn hơn hoặc bằng 0.")]
            public int? MaxStock { get; set; }

            public bool? IsOnSale { get; set; }

            [Range(1, int.MaxValue, ErrorMessage = "ID thương hiệu phải lớn hơn hoặc bằng 1.")]
            public int? BrandId { get; set; }

            [RegularExpression("^(asc|desc|avgasc|avgdesc)$", ErrorMessage = "Sắp xếp giá chỉ có thể là 'asc', 'desc', 'avgasc' hoặc 'avgdesc'.")]
            public string? SortByPrice { get; set; }
        }
        public class ProductDetailDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = null!;
            public decimal Price { get; set; }
            public decimal OriginalPrice { get; set; }
            public string? Description { get; set; }
            public string? ImageUrl { get; set; }
            public string? BrandName { get; set; }
            public List<string>? Categories { get; set; }
            public int CurrentStock { get; set; }
            public decimal? ShockPrice { get; set; }
        }
        // Request cho API tìm kiếm sản phẩm
        public class SearchProductsRequest
        {
            [Required(ErrorMessage = "Vui lòng nhập từ khóa tìm kiếm.")]
            public string Keyword { get; set; } = null!;

            [Range(1, int.MaxValue, ErrorMessage = "Số trang phải lớn hơn hoặc bằng 1.")]
            public int PageNumber { get; set; } = 1;

            [Range(1, int.MaxValue, ErrorMessage = "Số sản phẩm mỗi trang phải lớn hơn hoặc bằng 1.")]
            public int PageSize { get; set; } = 10;
        }


        // Request cho API lấy sản phẩm tương tự
        public class SimilarProductsRequest
        {
            [Range(1, int.MaxValue, ErrorMessage = "Số lượng sản phẩm tương tự phải lớn hơn 0.")]
            public int Top { get; set; } = 5;
        }

        // Request cho API lấy sản phẩm bán chạy nhất
        public class BestSellingProductsRequest
        {
            [Range(1, int.MaxValue, ErrorMessage = "Số lượng sản phẩm bán chạy nhất phải lớn hơn 0.")]
            public int Top { get; set; } = 10;
        }
    }
}