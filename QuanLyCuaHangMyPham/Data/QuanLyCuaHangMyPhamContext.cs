using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuanLyCuaHangMyPham.IdentityModels;
using QuanLyCuaHangMyPham.Models;

namespace QuanLyCuaHangMyPham.Data;

public partial class QuanLyCuaHangMyPhamContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
{
    public QuanLyCuaHangMyPhamContext()
    {
    }

    public QuanLyCuaHangMyPhamContext(DbContextOptions<QuanLyCuaHangMyPhamContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<BeautyBlog> BeautyBlogs { get; set; }

    public virtual DbSet<Brand> Brands { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Coupon> Coupons { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Favorite> Favorites { get; set; }

    public virtual DbSet<Inventory> Inventories { get; set; }

    public virtual DbSet<MembershipLevel> MembershipLevels { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductFeedback> ProductFeedbacks { get; set; }

    public virtual DbSet<Promotion> Promotions { get; set; }

    public virtual DbSet<ShippingCompany> ShippingCompanies { get; set; }

    public virtual DbSet<Staff> Staff { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {

    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        base.OnModelCreating(modelBuilder);

        // Cấu hình cho các bảng Identity (AspNet tables)
        modelBuilder.Entity<IdentityUserRole<int>>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId });
            entity.ToTable("UserRoles");
        });

        modelBuilder.Entity<IdentityUserLogin<int>>(entity =>
        {
            entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });
            entity.ToTable("UserLogins");
        });

        modelBuilder.Entity<IdentityUserToken<int>>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });
            entity.ToTable("UserTokens");
        });

        modelBuilder.Entity<IdentityUserClaim<int>>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("UserClaims");
        });
        // Cấu hình cho các thực thể khác

        // Cấu hình cho các thực thể khác (liên kết với bảng AspNetUsers)
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.AdminId).HasName("PK__Admins__43AA4141C99ADA52");

            entity.Property(e => e.AdminId).HasColumnName("admin_id");

            // Sửa kiểu dữ liệu từ "ntext" thành "nvarchar(max)"
            entity.Property(e => e.RoleDescription)
                  .HasColumnType("nvarchar(max)")
                  .HasColumnName("role_description");

            entity.Property(e => e.UserId).HasColumnName("user_id");

            // Liên kết với ApplicationUser thay vì bảng User
            entity.HasOne(d => d.User).WithMany(p => p.Admins)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Admins__user_id__48CFD27E");
        });

        modelBuilder.Entity<BeautyBlog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__BeautyBl__3213E83FA9E2408C");

            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.Author)
                .HasMaxLength(255)
                .IsUnicode(true) // Đảm bảo hỗ trợ Unicode cho tiếng Việt
                .HasColumnName("author");

            entity.Property(e => e.CategoryId).HasColumnName("category_id");

            // Sửa 'ntext' thành 'nvarchar(max)' để đảm bảo tính tương thích và hiệu suất
            entity.Property(e => e.Content)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("content");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");

            entity.Property(e => e.FeaturedImage)
                .HasMaxLength(255)
                .IsUnicode(true) // Đảm bảo ảnh đường dẫn hỗ trợ tiếng Việt
                .HasColumnName("featured_image");

            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .IsUnicode(true) // Tiêu đề cần hỗ trợ Unicode để ghi tiếng Việt
                .HasColumnName("title");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("updated_at");

            // Thiết lập quan hệ với bảng Category
            entity.HasOne(d => d.Category).WithMany(p => p.BeautyBlogs)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__BeautyBlo__categ__17036CC0");
        });

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Brands__3213E83F2F354363");

            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");

            // Thay thế 'ntext' bằng 'nvarchar(max)' để hỗ trợ Unicode và hiệu suất tốt hơn
            entity.Property(e => e.Description)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("description");

            // Đảm bảo 'LogoUrl' không cần hỗ trợ Unicode (vì là URL)
            entity.Property(e => e.LogoUrl)
                .HasMaxLength(255)
                .IsUnicode(false) // URL không cần hỗ trợ Unicode
                .HasColumnName("logo_url");

            // Sử dụng 'nvarchar' và 'IsUnicode(true)' để hỗ trợ tên thương hiệu bằng tiếng Việt
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnType("nvarchar(max)")
                .IsUnicode(true) // Đảm bảo hỗ trợ Unicode cho tiếng Việt
                .HasColumnName("name");
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.CartId).HasName("PK__Cart__2EF52A270DD78F5C");

            entity.ToTable("Cart");

            entity.Property(e => e.CartId).HasColumnName("cart_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.LastUpdated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("last_updated");

            entity.HasOne(d => d.Customer).WithMany(p => p.Carts)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK__Cart__customer_i__693CA210");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.CartItemId).HasName("PK__CartItem__5D9A6C6E4E1BFD8A");

            entity.Property(e => e.CartItemId).HasColumnName("cart_item_id");
            entity.Property(e => e.AddedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("added_at");
            entity.Property(e => e.CartId).HasColumnName("cart_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");

            entity.HasOne(d => d.Cart).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.CartId)
                .HasConstraintName("FK__CartItems__cart___6D0D32F4");

            entity.HasOne(d => d.Product).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__CartItems__produ__6E01572D");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Categori__3213E83F14804A61");

            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");

            // Sửa 'ntext' thành 'nvarchar(max)' để hỗ trợ Unicode và đảm bảo hiệu suất
            entity.Property(e => e.Description)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("description");

            // Đảm bảo tên hỗ trợ Unicode cho tiếng Việt
            entity.Property(e => e.Name)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("name");

            entity.Property(e => e.ParentId).HasColumnName("parent_id");

            // Thiết lập quan hệ tự tham chiếu
            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
                .HasForeignKey(d => d.ParentId)
                .OnDelete(DeleteBehavior.SetNull) // Đảm bảo không lỗi khi Parent bị xóa
                .HasConstraintName("FK__Categorie__paren__5070F446");
        });

        modelBuilder.Entity<Coupon>(entity =>
        {
            // Khóa chính
            entity.HasKey(e => e.Id).HasName("PK__Coupons__3213E83F505ACA10");

            // Thiết lập chỉ mục duy nhất cho mã coupon
            entity.HasIndex(e => e.Code, "UQ__Coupons__357D4CF99D5DF3F0").IsUnique();

            // Cột Id
            entity.Property(e => e.Id).HasColumnName("id");

            // Mã coupon (Code) với hỗ trợ Unicode
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsUnicode(false) // Hỗ trợ tiếng Việt và các ký tự đặc biệt
                .HasColumnName("code");

            // Ngày tạo coupon
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");

            // Số tiền giảm giá
            entity.Property(e => e.DiscountAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("discount_amount");

            // Phần trăm giảm giá
            entity.Property(e => e.DiscountPercentage)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("discount_percentage");

            // Ngày kết thúc hiệu lực của coupon
            entity.Property(e => e.EndDate).HasColumnName("end_date");

            // Số tiền giảm tối đa
            entity.Property(e => e.MaxDiscountAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("max_discount_amount");

            // Số tiền đơn hàng tối thiểu để áp dụng coupon
            entity.Property(e => e.MinimumOrderAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("minimum_order_amount");

            // Tên coupon với hỗ trợ Unicode
            entity.Property(e => e.Name)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("name");

            // Số lượng coupon khả dụng
            entity.Property(e => e.QuantityAvailable)
                .HasDefaultValue(0)
                .HasColumnName("quantity_available");

            // Ngày bắt đầu hiệu lực của coupon
            entity.Property(e => e.StartDate).HasColumnName("start_date");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            // Định nghĩa khóa chính
            entity.HasKey(e => e.CustomerId).HasName("PK__Customer__CD65CB8580B29AEF");

            // Cấu hình thuộc tính CustomerId
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");

            // Cấu hình thuộc tính Address để hỗ trợ lưu tiếng Việt
            entity.Property(e => e.Address)
                .HasColumnType("nvarchar(max)")  // Thay 'ntext' bằng 'nvarchar(max)'
                .HasColumnName("address");

            // Cấu hình thuộc tính CreatedAt với giá trị mặc định là ngày hiện tại
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");

            // Cấu hình MembershipLevelId
            entity.Property(e => e.MembershipLevelId).HasColumnName("membership_level_id");

            // Cấu hình TotalSpending với giá trị mặc định là 0 và kiểu decimal
            entity.Property(e => e.TotalSpending)
                .HasDefaultValue(0.00m)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("total_spending");

            // Cấu hình UserId
            entity.Property(e => e.UserId).HasColumnName("user_id");

            // Thiết lập quan hệ với MembershipLevel
            entity.HasOne(d => d.MembershipLevel).WithMany(p => p.Customers)
                .HasForeignKey(d => d.MembershipLevelId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__Customers__membe__45F365D3");

            // Thiết lập quan hệ với ApplicationUser
            entity.HasOne(d => d.User).WithMany(p => p.Customers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Customers__user___44FF419A");
        });

        modelBuilder.Entity<Favorite>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.ProductId }).HasName("PK__Favorite__FDCE10D0D9F0ECED");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.AddedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("added_at");

            entity.HasOne(d => d.Product).WithMany(p => p.Favorites)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__Favorites__produ__07C12930");

            entity.HasOne(d => d.User).WithMany(p => p.Favorites)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Favorites__user___06CD04F7");
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.InventoryId).HasName("PK__Inventor__B59ACC49FDC07770");

            // Bảng này có trigger
            entity.ToTable("Inventory", tb => tb.HasTrigger("trg_notify_low_stock"));

            // Cột InventoryId
            entity.Property(e => e.InventoryId).HasColumnName("inventory_id");

            // Ngày cập nhật gần nhất
            entity.Property(e => e.LastUpdated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("last_updated");

            // Khóa ngoại đến Product
            entity.Property(e => e.ProductId).HasColumnName("product_id");

            // Số lượng tồn kho
            entity.Property(e => e.QuantityInStock).HasColumnName("quantity_in_stock");

            // Vị trí kho với hỗ trợ Unicode
            entity.Property(e => e.WarehouseLocation)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("warehouse_location");

            // Quan hệ giữa Inventory và Product
            entity.HasOne(d => d.Product).WithMany(p => p.Inventories)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__Inventory__produ__6477ECF3");
        });

        modelBuilder.Entity<MembershipLevel>(entity =>
        {
            // Thiết lập khóa chính
            entity.HasKey(e => e.MembershipLevelId).HasName("PK__Membersh__0202741249F9C39C");

            // Cột MembershipLevelId
            entity.Property(e => e.MembershipLevelId).HasColumnName("membership_level_id");

            // Lợi ích (Benefits) với hỗ trợ Unicode
            entity.Property(e => e.Benefits)
                .HasColumnType("nvarchar(MAX)") // Đổi từ 'text' sang 'nvarchar(MAX)'
                .HasColumnName("benefits");

            // Ngày tạo
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");

            // Tên cấp độ thành viên với hỗ trợ Unicode
            entity.Property(e => e.LevelName)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("level_name");

            // Số tiền chi tiêu tối thiểu để đạt cấp độ này
            entity.Property(e => e.MinimumSpending)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("minimum_spending");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            // Định nghĩa khóa chính
            entity.HasKey(e => e.Id).HasName("PK__Orders__3213E83F03E2C29E");

            // Đính kèm các trigger
            entity.ToTable(tb =>
            {
                tb.HasTrigger("trg_apply_coupon_discount");
                tb.HasTrigger("trg_auto_cancel_order");
                tb.HasTrigger("trg_calculate_tax");
                tb.HasTrigger("trg_check_coupon_validity");
                tb.HasTrigger("trg_fill_shipping_info");
                tb.HasTrigger("trg_update_coupon_quantity");
                tb.HasTrigger("trg_update_inventory_after_shipping");
                tb.HasTrigger("trg_update_payment_status");
                tb.HasTrigger("trg_update_total_spending");
            });

            // Tạo các chỉ mục (indexes)
            entity.HasIndex(e => e.CustomerId, "idx_orders_customer_id");
            entity.HasIndex(e => e.OrderDate, "idx_orders_order_date");

            // Cấu hình các thuộc tính
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CouponId).HasColumnName("coupon_id");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");

            entity.Property(e => e.DiscountApplied)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("discount_applied");

            entity.Property(e => e.EstimatedDeliveryDate).HasColumnName("estimated_delivery_date");

            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("order_date");

            // Ghi chú đơn hàng, hỗ trợ tiếng Việt
            entity.Property(e => e.OrderNotes)
                .HasColumnType("nvarchar(MAX)") // Đổi từ text sang nvarchar(MAX)
                .HasColumnName("order_notes");

            entity.Property(e => e.OriginalTotalAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("original_total_amount");

            entity.Property(e => e.PaymentMethodId).HasColumnName("payment_method_id");

            // Trạng thái thanh toán, hỗ trợ tiếng Việt
            entity.Property(e => e.PaymentStatus)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("payment_status");

            // Địa chỉ giao hàng, hỗ trợ tiếng Việt
            entity.Property(e => e.ShippingAddress)
                .HasColumnType("nvarchar(MAX)") // Đổi từ text sang nvarchar(MAX)
                .HasColumnName("shipping_address");

            entity.Property(e => e.ShippingCompanyId).HasColumnName("shipping_company_id");

            entity.Property(e => e.ShippingCost)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("shipping_cost");

            // Phương thức giao hàng, hỗ trợ tiếng Việt
            entity.Property(e => e.ShippingMethod)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("shipping_method");

            // Trạng thái đơn hàng, hỗ trợ tiếng Việt
            entity.Property(e => e.Status)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("status");

            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("total_amount");

            // Thiết lập quan hệ với bảng Coupon
            entity.HasOne(d => d.Coupon).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CouponId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__Orders__coupon_i__7F2BE32F");

            // Thiết lập quan hệ với bảng Customer
            entity.HasOne(d => d.Customer).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK__Orders__customer__7C4F7684");

            // Thiết lập quan hệ với bảng PaymentMethod
            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.Orders)
                .HasForeignKey(d => d.PaymentMethodId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__Orders__payment___7D439ABD");

            // Thiết lập quan hệ với bảng ShippingCompany
            entity.HasOne(d => d.ShippingCompany).WithMany(p => p.Orders)
                .HasForeignKey(d => d.ShippingCompanyId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__Orders__shipping__7E37BEF6");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            // Định nghĩa khóa chính
            entity.HasKey(e => e.Id).HasName("PK__OrderDet__3213E83F5509C535");

            // Tạo chỉ mục cho OrderId để tối ưu hóa tìm kiếm
            entity.HasIndex(e => e.OrderId, "idx_orderdetails_order_id");

            // Cấu hình các thuộc tính
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");

            // Cấu hình thuộc tính ProductVariation
            entity.Property(e => e.ProductVariation)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("product_variation");

            entity.Property(e => e.Quantity).HasColumnName("quantity");

            // Cấu hình TotalPrice với công thức tính
            entity.Property(e => e.TotalPrice)
                .HasComputedColumnSql("([quantity]*[unit_price])", true)
                .HasColumnType("decimal(21, 2)")
                .HasColumnName("total_price");

            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("unit_price");

            // Thiết lập quan hệ với bảng Order
            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__OrderDeta__order__02084FDA");

            // Thiết lập quan hệ với bảng Product
            entity.HasOne(d => d.Product).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__OrderDeta__produ__02FC7413");
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            // Định nghĩa khóa chính
            entity.HasKey(e => e.Id).HasName("PK__PaymentM__3213E83FD39C27F1");

            // Cấu hình các thuộc tính
            entity.Property(e => e.Id).HasColumnName("id");

            // Thuộc tính ngày tạo với giá trị mặc định
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");

            // Thuộc tính Description với hỗ trợ Unicode
            entity.Property(e => e.Description)
                .HasColumnType("nvarchar(max)") // Thay thế 'ntext' bằng 'nvarchar(max)'
                .HasColumnName("description");

            // Thuộc tính ImageUrl
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .IsUnicode(false) // Không cần hỗ trợ Unicode cho URL
                .HasColumnName("image_url");

            // Thuộc tính Name với hỗ trợ Unicode
            entity.Property(e => e.Name)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("name");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            // Định nghĩa khóa chính
            entity.HasKey(e => e.Id).HasName("PK__Products__3213E83FE376E46C");

            // Tạo chỉ mục cho cột Id để tối ưu truy vấn
            entity.HasIndex(e => e.Id, "idx_products_product_id");

            // Cấu hình thuộc tính Id
            entity.Property(e => e.Id).HasColumnName("id");

            // Thuộc tính AverageRating với giá trị mặc định
            entity.Property(e => e.AverageRating)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(3, 2)")
                .HasColumnName("average_rating");

            // Thuộc tính BrandId với liên kết khóa ngoại
            entity.Property(e => e.BrandId).HasColumnName("brand_id");

            // Thuộc tính CreatedAt với giá trị mặc định là thời gian hiện tại
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");

            // Thuộc tính Description với hỗ trợ Unicode
            entity.Property(e => e.Description)
                .HasColumnType("nvarchar(max)") // Thay 'ntext' bằng 'nvarchar(max)'
                .HasColumnName("description");

            // Thuộc tính FavoriteCount với giá trị mặc định
            entity.Property(e => e.FavoriteCount)
                .HasDefaultValue(0)
                .HasColumnName("favorite_count");

            // Thuộc tính ImageUrl không cần hỗ trợ Unicode
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .IsUnicode(false) // Không cần Unicode cho URL
                .HasColumnName("image_url");

            // Thuộc tính Name hỗ trợ Unicode
            entity.Property(e => e.Name)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("name");

            // Thuộc tính OriginalPrice và Price với định dạng tiền tệ
            entity.Property(e => e.OriginalPrice)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("original_price");

            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");

            // Thuộc tính ReviewCount với giá trị mặc định
            entity.Property(e => e.ReviewCount)
                .HasDefaultValue(0)
                .HasColumnName("review_count");

            // Cấu hình khóa ngoại với Brand
            entity.HasOne(d => d.Brand).WithMany(p => p.Products)
                .HasForeignKey(d => d.BrandId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__Products__brand___5BE2A6F2");

            // Cấu hình liên kết nhiều-nhiều giữa Product và Category
            entity.HasMany(d => d.Categories).WithMany(p => p.Products)
                .UsingEntity<Dictionary<string, object>>(
                    "ProductCategory",
                    r => r.HasOne<Category>().WithMany()
                        .HasForeignKey("CategoryId")
                        .HasConstraintName("FK__ProductCa__categ__5FB337D6"),
                    l => l.HasOne<Product>().WithMany()
                        .HasForeignKey("ProductId")
                        .HasConstraintName("FK__ProductCa__produ__5EBF139D"),
                    j =>
                    {
                        j.HasKey("ProductId", "CategoryId").HasName("PK__ProductC__1A56936EC24E0177");
                        j.ToTable("ProductCategories");
                        j.IndexerProperty<int>("ProductId").HasColumnName("product_id");
                        j.IndexerProperty<int>("CategoryId").HasColumnName("category_id");
                    });
        });

        modelBuilder.Entity<ProductFeedback>(entity =>
        {
            // Khóa chính cho bảng ProductFeedback
            entity.HasKey(e => e.FeedbackId).HasName("PK__ProductF__7A6B2B8C9FF7CC51");

            // Tên bảng
            entity.ToTable("ProductFeedback");

            // Thuộc tính FeedbackId
            entity.Property(e => e.FeedbackId).HasColumnName("feedback_id");

            // Thuộc tính CustomerId với khóa ngoại
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");

            // Thuộc tính FeedbackDate với giá trị mặc định là ngày hiện tại
            entity.Property(e => e.FeedbackDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("feedback_date");

            // Thuộc tính ProductId với khóa ngoại
            entity.Property(e => e.ProductId).HasColumnName("product_id");

            // Thuộc tính Rating
            entity.Property(e => e.Rating).HasColumnName("rating");

            // Thuộc tính ReviewText hỗ trợ Unicode cho đánh giá
            entity.Property(e => e.ReviewText)
                .HasColumnType("nvarchar(max)") // Thay 'ntext' bằng 'nvarchar(max)'
                .HasColumnName("review_text");

            // Khóa ngoại đến Customer
            entity.HasOne(d => d.Customer).WithMany(p => p.ProductFeedbacks)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK__ProductFe__custo__114A936A");

            // Khóa ngoại đến Product
            entity.HasOne(d => d.Product).WithMany(p => p.ProductFeedbacks)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__ProductFe__produ__123EB7A3");
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            // Khóa chính cho bảng Promotion
            entity.HasKey(e => e.Id).HasName("PK__Promotio__3213E83FB4EFB36C");

            // Định nghĩa các thuộc tính
            entity.Property(e => e.Id).HasColumnName("id");

            // Thuộc tính CreatedAt với giá trị mặc định là ngày hiện tại
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");

            // Thuộc tính DiscountPercentage với kiểu decimal
            entity.Property(e => e.DiscountPercentage)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("discount_percentage");

            // Thuộc tính EndDate
            entity.Property(e => e.EndDate).HasColumnName("end_date");

            // Thuộc tính Name với hỗ trợ Unicode để lưu trữ tên khuyến mãi bằng tiếng Việt
            entity.Property(e => e.Name)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("name");

            // Thuộc tính ProductId (khóa ngoại)
            entity.Property(e => e.ProductId).HasColumnName("product_id");

            // Thuộc tính StartDate
            entity.Property(e => e.StartDate).HasColumnName("start_date");

            // Khóa ngoại đến bảng Product, thiết lập Cascade Delete
            entity.HasOne(d => d.Product).WithMany(p => p.Promotions)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade) // Xóa sản phẩm sẽ xóa luôn các khuyến mãi liên quan
                .HasConstraintName("FK__Promotion__produ__0C85DE4D");
        });

        modelBuilder.Entity<ShippingCompany>(entity =>
        {
            // Khóa chính của bảng ShippingCompany
            entity.HasKey(e => e.Id).HasName("PK__Shipping__3213E83F51415519");

            // Định nghĩa các thuộc tính
            entity.Property(e => e.Id).HasColumnName("id");

            // Thuộc tính CreatedAt với giá trị mặc định là ngày hiện tại
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");

            // Thuộc tính ImageUrl (URL hình ảnh) không cần Unicode
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .IsUnicode(false) // URL thường không cần hỗ trợ Unicode
                .HasColumnName("image_url");

            // Thuộc tính Name, cấu hình hỗ trợ Unicode cho tên công ty vận chuyển
            entity.Property(e => e.Name)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("name");

            // Thuộc tính ShippingCost, cấu hình kiểu decimal cho chi phí vận chuyển
            entity.Property(e => e.ShippingCost)
                .HasColumnType("decimal(10, 2)") // Hỗ trợ lưu giá trị tiền tệ chính xác
                .HasColumnName("shipping_cost");
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            // Định nghĩa khóa chính của bảng Staff
            entity.HasKey(e => e.StaffId).HasName("PK__Staff__1963DD9C4B3F26DF");

            // Thuộc tính StaffId là khóa chính
            entity.Property(e => e.StaffId).HasColumnName("staff_id");

            // Thuộc tính HireDate với giá trị mặc định là ngày hiện tại
            entity.Property(e => e.HireDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("hire_date");

            // Thuộc tính Position (Chức vụ) hỗ trợ Unicode cho các ký tự tiếng Việt
            entity.Property(e => e.Position)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("position");

            // Thuộc tính UserId để liên kết với ApplicationUser
            entity.Property(e => e.UserId).HasColumnName("user_id");

            // Thiết lập mối quan hệ với ApplicationUser
            entity.HasOne(d => d.User).WithMany(p => p.Staffs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Staff__user_id__4CA06362");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
