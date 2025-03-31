// CartRepository.cs
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using QuanLyCuaHangMyPham.Data;
using QuanLyCuaHangMyPham.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyCuaHangMyPham.Repositories.Cart
{
    public class CartRepository
    {
        private readonly QuanLyCuaHangMyPhamContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartRepository(QuanLyCuaHangMyPhamContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        #region User Cart Methods

        // Phương thức cho AddToCartCommand
        public async Task<bool> AddToCart(int userId, int productId, int quantity)
        {
            try
            {
                // Tìm thông tin khách hàng
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (customer == null)
                    return false;

                // Lấy giỏ hàng của khách hàng
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId);

                // Nếu chưa có giỏ hàng, tạo mới
                if (cart == null)
                {
                    cart = new Models.Cart
                    {
                        CustomerId = customer.CustomerId,
                        CreatedAt = DateTime.Now,
                        LastUpdated = DateTime.Now,
                        CartItems = new List<CartItem>()
                    };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                // Kiểm tra sản phẩm đã có trong giỏ hàng chưa
                var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
                if (existingItem != null)
                {
                    // Nếu đã có, cập nhật số lượng
                    existingItem.Quantity += quantity;
                }
                else
                {
                    // Nếu chưa có, thêm mới
                    cart.CartItems.Add(new CartItem
                    {
                        CartId = cart.CartId,
                        ProductId = productId,
                        Quantity = quantity,
                        AddedAt = DateTime.Now
                    });
                }

                cart.LastUpdated = DateTime.Now;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Phương thức cho RemoveFromCartCommand
        public async Task<bool> RemoveFromCart(int userId, int productId, int quantity)
        {
            try
            {
                // Lấy thông tin Customer từ UserId
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (customer == null)
                    return false;

                // Lấy giỏ hàng của khách hàng
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId);

                if (cart == null)
                    return false;

                // Tìm sản phẩm trong giỏ hàng
                var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
                if (existingItem != null)
                {
                    // Giảm số lượng sản phẩm
                    existingItem.Quantity -= quantity;
                    if (existingItem.Quantity <= 0)
                    {
                        // Nếu số lượng <= 0, xóa sản phẩm khỏi giỏ hàng
                        cart.CartItems.Remove(existingItem);
                    }

                    cart.LastUpdated = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        // Phương thức cho UpdateCartCommand
        public async Task<bool> UpdateCart(int userId, int productId, int quantity)
        {
            try
            {
                // Lấy thông tin Customer từ UserId
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (customer == null)
                    return false;

                // Lấy giỏ hàng của khách hàng
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId);

                if (cart == null)
                    return false;

                // Kiểm tra sản phẩm đã có trong giỏ hàng chưa
                var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
                if (existingItem != null)
                {
                    // Cập nhật số lượng sản phẩm
                    existingItem.Quantity = quantity;

                    if (existingItem.Quantity <= 0)
                    {
                        // Nếu số lượng <= 0, xóa sản phẩm khỏi giỏ hàng
                        cart.CartItems.Remove(existingItem);
                    }

                    cart.LastUpdated = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        // Phương thức cho GetCartDetailsCommand
        public async Task<(bool success, object cartDetails)> GetCartDetails(int userId)
        {
            try
            {
                // Lấy thông tin khách hàng từ UserId
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (customer == null)
                    return (false, null);

                // Lấy giỏ hàng của khách hàng
                var cartItems = await _context.CartItems
                    .Include(ci => ci.Product)
                    .Where(ci => ci.Cart.CustomerId == customer.CustomerId)
                    .Select(ci => new
                    {
                        ProductId = ci.ProductId,
                        ProductName = ci.Product.Name,
                        Quantity = ci.Quantity,
                        UnitPrice = ci.Product.Price,
                        TotalPrice = ci.Quantity * ci.Product.Price,
                        ImageUrl = ci.Product.ImageUrl
                    })
                    .ToListAsync();

                if (!cartItems.Any())
                {
                    return (true, new
                    {
                        CartItems = new List<object>(),
                        TotalAmount = 0
                    });
                }

                // Tính tổng tiền của giỏ hàng
                var totalAmount = cartItems.Sum(ci => ci.TotalPrice);

                return (true, new
                {
                    CartItems = cartItems,
                    TotalAmount = totalAmount
                });
            }
            catch
            {
                return (false, null);
            }
        }

        // Phương thức cho RemoveItemCommand
        public async Task<bool> RemoveItem(int userId, int productId)
        {
            try
            {
                // Lấy thông tin Customer từ UserId
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (customer == null)
                    return false;

                // Lấy giỏ hàng của khách hàng
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId);

                if (cart == null)
                    return false;

                // Tìm sản phẩm trong giỏ hàng
                var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
                if (existingItem != null)
                {
                    // Xóa sản phẩm khỏi giỏ hàng
                    cart.CartItems.Remove(existingItem);
                    cart.LastUpdated = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        // Phương thức cho ClearCartCommand
        public async Task<bool> ClearCart(int userId)
        {
            try
            {
                // Lấy thông tin Customer từ UserId
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (customer == null)
                    return false;

                // Lấy giỏ hàng của khách hàng
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId);

                if (cart == null)
                    return false;

                cart.CartItems.Clear();
                cart.LastUpdated = DateTime.Now;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Phương thức cho PreviewOrderCommand
        public async Task<(bool success, object previewData)> PreviewOrder(
            int userId, string couponCode, int? shippingCompanyId,
            int? paymentMethodId, string shippingAddress, string phoneNumber, string email)
        {
            try
            {
                var customer = await _context.Customers
                    .Include(c => c.MembershipLevel)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (customer == null)
                    return (false, null);

                var cartItems = await _context.CartItems
                    .Include(ci => ci.Product)
                    .Where(ci => ci.Cart.CustomerId == customer.CustomerId)
                    .ToListAsync();

                if (!cartItems.Any())
                    return (false, null);

                decimal originalTotalAmount = cartItems.Sum(ci => ci.Product.Price * ci.Quantity);
                decimal discountAmount = 0;

                // Áp dụng giảm giá từ hạng thành viên
                if (customer.MembershipLevel != null)
                {
                    discountAmount = originalTotalAmount * (customer.MembershipLevel.DiscountRate / 100);
                }

                // Áp dụng mã giảm giá nếu có
                if (!string.IsNullOrEmpty(couponCode))
                {
                    var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == couponCode);
                    if (coupon != null && coupon.QuantityAvailable > 0)
                    {
                        discountAmount = coupon.DiscountAmount ?? (coupon.DiscountPercentage.HasValue
                            ? originalTotalAmount * (coupon.DiscountPercentage.Value / 100)
                            : 0);

                        if (coupon.MaxDiscountAmount.HasValue && discountAmount > coupon.MaxDiscountAmount.Value)
                        {
                            discountAmount = coupon.MaxDiscountAmount.Value;
                        }
                    }
                    else
                    {
                        return (false, "Mã giảm giá không hợp lệ hoặc đã hết số lượng.");
                    }
                }

                // Tính phí vận chuyển
                decimal shippingCost = 0;
                if (shippingCompanyId.HasValue)
                {
                    var shippingCompany = await _context.ShippingCompanies
                        .FirstOrDefaultAsync(sc => sc.Id == shippingCompanyId.Value);

                    if (shippingCompany != null)
                    {
                        shippingCost = shippingCompany.ShippingCost ?? 0;
                    }
                }

                decimal totalAmount = originalTotalAmount - discountAmount + shippingCost;

                // Dữ liệu preview
                var cartDetails = cartItems.Select(ci => new
                {
                    ProductId = ci.ProductId,
                    ProductName = ci.Product.Name,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.Product.Price,
                    TotalPrice = ci.Quantity * ci.Product.Price,
                    ImageUrl = ci.Product.ImageUrl
                }).ToList();

                var previewData = new
                {
                    CartItems = cartDetails,
                    OriginalTotalAmount = originalTotalAmount,
                    DiscountAmount = discountAmount,
                    ShippingCost = shippingCost,
                    TotalAmount = totalAmount,
                    CouponCode = couponCode,
                    ShippingCompanyId = shippingCompanyId,
                    PaymentMethodId = paymentMethodId,
                    ShippingAddress = shippingAddress,
                    PhoneNumber = phoneNumber,
                    Email = email
                };

                return (true, previewData);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        #endregion

        #region Guest Cart Methods

        // Phương thức cho AddToGuestCartCommand
        public async Task<(bool success, List<CartItem> cartItems)> AddToGuestCart(int productId, int quantity)
        {
            try
            {
                var session = _httpContextAccessor.HttpContext.Session;

                // Lấy giỏ hàng từ session
                var cartItemsJson = session.GetString("CartItems");
                List<CartItem> cartItems = string.IsNullOrEmpty(cartItemsJson)
                    ? new List<CartItem>()
                    : JsonConvert.DeserializeObject<List<CartItem>>(cartItemsJson);

                // Kiểm tra sản phẩm đã tồn tại chưa
                var existingItem = cartItems.FirstOrDefault(ci => ci.ProductId == productId);
                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                }
                else
                {
                    cartItems.Add(new CartItem
                    {
                        ProductId = productId,
                        Quantity = quantity,
                        AddedAt = DateTime.Now
                    });
                }

                // Lưu giỏ hàng vào session
                cartItemsJson = JsonConvert.SerializeObject(cartItems);
                session.SetString("CartItems", cartItemsJson);

                return (true, cartItems);
            }
            catch
            {
                return (false, null);
            }
        }

        // Phương thức cho RemoveFromGuestCartCommand
        public async Task<(bool success, List<CartItem> cartItems)> RemoveFromGuestCart(int productId, int quantity)
        {
            try
            {
                var session = _httpContextAccessor.HttpContext.Session;

                // Lấy giỏ hàng từ session
                var cartItemsJson = session.GetString("CartItems");
                if (string.IsNullOrEmpty(cartItemsJson))
                {
                    return (false, null);
                }

                var cartItems = JsonConvert.DeserializeObject<List<CartItem>>(cartItemsJson);

                // Tìm sản phẩm trong giỏ hàng
                var existingItem = cartItems.FirstOrDefault(ci => ci.ProductId == productId);
                if (existingItem != null)
                {
                    // Giảm số lượng sản phẩm
                    existingItem.Quantity -= quantity;
                    if (existingItem.Quantity <= 0)
                    {
                        // Nếu số lượng <= 0, xóa sản phẩm khỏi giỏ hàng
                        cartItems.Remove(existingItem);
                    }

                    // Lưu giỏ hàng vào session
                    session.SetString("CartItems", JsonConvert.SerializeObject(cartItems));

                    return (true, cartItems);
                }

                return (false, null);
            }
            catch
            {
                return (false, null);
            }
        }

        // Phương thức cho UpdateGuestCartCommand
        public async Task<(bool success, List<CartItem> cartItems)> UpdateGuestCart(int productId, int quantity)
        {
            try
            {
                var session = _httpContextAccessor.HttpContext.Session;

                // Lấy giỏ hàng từ session
                var cartItemsJson = session.GetString("CartItems");
                if (string.IsNullOrEmpty(cartItemsJson))
                {
                    return (false, null);
                }

                var cartItems = JsonConvert.DeserializeObject<List<CartItem>>(cartItemsJson);

                // Kiểm tra sản phẩm đã tồn tại trong giỏ hàng chưa
                var existingItem = cartItems.FirstOrDefault(ci => ci.ProductId == productId);
                if (existingItem != null)
                {
                    // Cập nhật số lượng sản phẩm
                    existingItem.Quantity = quantity;

                    if (existingItem.Quantity <= 0)
                    {
                        cartItems.Remove(existingItem);
                    }

                    // Lưu giỏ hàng vào session
                    session.SetString("CartItems", JsonConvert.SerializeObject(cartItems));

                    return (true, cartItems);
                }

                return (false, null);
            }
            catch
            {
                return (false, null);
            }
        }

        // Phương thức cho GetGuestCartDetailsCommand
        public async Task<(bool success, object cartDetails)> GetGuestCartDetails()
        {
            try
            {
                var session = _httpContextAccessor.HttpContext.Session;

                // Lấy giỏ hàng từ session
                var cartItemsJson = session.GetString("CartItems");

                if (string.IsNullOrEmpty(cartItemsJson))
                {
                    return (true, new
                    {
                        CartItems = new List<object>(),
                        TotalAmount = 0
                    });
                }

                // Deserialize giỏ hàng từ session
                var cartItems = JsonConvert.DeserializeObject<List<CartItem>>(cartItemsJson);

                if (cartItems == null || !cartItems.Any())
                {
                    return (true, new
                    {
                        CartItems = new List<object>(),
                        TotalAmount = 0
                    });
                }

                // Lấy danh sách ID sản phẩm từ session
                var productIds = cartItems.Select(ci => ci.ProductId).Distinct().ToList();

                // Tạo chuỗi ID cho câu truy vấn SQL
                string idList = string.Join(",", productIds);

                var products = await _context.Products
                    .FromSqlRaw($"SELECT * FROM Products WHERE id IN ({idList})")
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Price,
                        p.ImageUrl
                    })
                    .ToListAsync();

                // Tạo chi tiết giỏ hàng
                var cartDetails = cartItems
                    .Join(products,
                        ci => ci.ProductId,
                        p => p.Id,
                        (cartItem, product) => new
                        {
                            ProductId = cartItem.ProductId,
                            ProductName = product.Name,
                            Quantity = cartItem.Quantity,
                            UnitPrice = product.Price,
                            TotalPrice = cartItem.Quantity * product.Price,
                            ImageUrl = product.ImageUrl
                        })
                    .ToList();

                // Tính tổng tiền giỏ hàng
                var totalAmount = cartDetails.Sum(ci => ci.TotalPrice);

                return (true, new
                {
                    CartItems = cartDetails,
                    TotalAmount = totalAmount
                });
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        // Phương thức cho ClearGuestCartCommand
        public async Task<bool> ClearGuestCart()
        {
            try
            {
                var session = _httpContextAccessor.HttpContext.Session;

                // Lấy giỏ hàng từ session
                var cartItemsJson = session.GetString("CartItems");
                if (string.IsNullOrEmpty(cartItemsJson))
                {
                    return false;
                }

                var cartItems = JsonConvert.DeserializeObject<List<CartItem>>(cartItemsJson);

                // Xóa tất cả sản phẩm trong giỏ hàng
                cartItems.Clear();

                // Lưu giỏ hàng trống vào session
                session.SetString("CartItems", JsonConvert.SerializeObject(cartItems));

                return true;
            }
            catch
            {
                return false;
            }
        }

        // Phương thức cho PreviewGuestOrderCommand
        public async Task<(bool success, object previewData)> PreviewGuestOrder(
            string couponCode, int? shippingCompanyId, int? paymentMethodId,
            string shippingAddress, string phoneNumber, string email)
        {
            try
            {
                var session = _httpContextAccessor.HttpContext.Session;

                // Lấy giỏ hàng từ session
                var cartItemsJson = session.GetString("CartItems");
                if (string.IsNullOrEmpty(cartItemsJson))
                {
                    return (false, "Giỏ hàng trống");
                }

                // Deserialize giỏ hàng từ session
                var cartItems = JsonConvert.DeserializeObject<List<CartItem>>(cartItemsJson);

                if (cartItems == null || !cartItems.Any())
                {
                    return (false, "Giỏ hàng trống");
                }

                // Lấy danh sách ID sản phẩm
                var productIds = cartItems.Select(ci => ci.ProductId).Distinct().ToList();

                // Lấy thông tin sản phẩm từ database
                var products = await _context.Products
                    .Where(p => productIds.Contains(p.Id))
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Price,
                        p.ImageUrl
                    })
                    .ToListAsync();

                // Tạo chi tiết giỏ hàng
                var cartDetails = cartItems
                    .Join(products,
                        ci => ci.ProductId,
                        p => p.Id,
                        (cartItem, product) => new
                        {
                            ProductId = cartItem.ProductId,
                            ProductName = product.Name,
                            Quantity = cartItem.Quantity,
                            UnitPrice = product.Price,
                            TotalPrice = cartItem.Quantity * product.Price,
                            ImageUrl = product.ImageUrl
                        })
                    .ToList();

                // Tính tổng tiền gốc
                decimal originalTotalAmount = cartDetails.Sum(ci => ci.TotalPrice);

                // Xử lý giảm giá
                decimal discountAmount = 0;
                if (!string.IsNullOrEmpty(couponCode))
                {
                    var coupon = await _context.Coupons
                        .FirstOrDefaultAsync(c => c.Code == couponCode);

                    if (coupon != null && coupon.QuantityAvailable > 0)
                    {
                        discountAmount = coupon.DiscountAmount ??
                            (coupon.DiscountPercentage.HasValue
                                ? originalTotalAmount * (coupon.DiscountPercentage.Value / 100m)
                                : 0);

                        if (coupon.MaxDiscountAmount.HasValue)
                        {
                            discountAmount = Math.Min(discountAmount, coupon.MaxDiscountAmount.Value);
                        }
                    }
                    else
                    {
                        return (false, "Mã giảm giá không hợp lệ hoặc đã hết số lượng.");
                    }
                }

                // Xử lý phí vận chuyển
                decimal shippingCost = 0;
                if (shippingCompanyId.HasValue)
                {
                    var shippingCompany = await _context.ShippingCompanies
                        .FirstOrDefaultAsync(sc => sc.Id == shippingCompanyId.Value);

                    if (shippingCompany != null)
                    {
                        shippingCost = shippingCompany.ShippingCost ?? 0;
                    }
                }

                // Tính tổng tiền cuối cùng
                decimal totalAmount = originalTotalAmount - discountAmount + shippingCost;

                // Tạo đối tượng preview
                var previewData = new
                {
                    CartItems = cartDetails,
                    OriginalTotalAmount = originalTotalAmount,
                    DiscountAmount = discountAmount,
                    ShippingCost = shippingCost,
                    TotalAmount = totalAmount,
                    CouponCode = couponCode,
                    ShippingCompanyId = shippingCompanyId,
                    PaymentMethodId = paymentMethodId,
                    ShippingAddress = shippingAddress,
                    PhoneNumber = phoneNumber,
                    Email = email
                };

                // Lưu thông tin preview vào session
                session.SetString("PreviewOrderData", JsonConvert.SerializeObject(previewData));

                return (true, previewData);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        #endregion
    }
}
