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

    public virtual DbSet<ChatConversation> ChatConversations { get; set; }

    public virtual DbSet<ChatMessage> ChatMessages { get; set; }

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

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=TEENIA_PC;Database=QuanLyCuaHangMyPham;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        base.OnModelCreating(modelBuilder);

        // Cấu hình cho IdentityUserRole<int>
        modelBuilder.Entity<IdentityUserRole<int>>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId });  // Đảm bảo rằng bạn có khóa chính
            entity.ToTable("UserRoles");  // Đặt tên bảng nếu muốn thay đổi

            // Cấu hình thêm nếu cần
        });
        modelBuilder.Entity<IdentityUserLogin<int>>(entity =>
        {
            entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });  // Khóa chính là kết hợp của LoginProvider và ProviderKey
            entity.ToTable("UserLogins");  // Đặt tên bảng nếu muốn thay đổi
        });
        modelBuilder.Entity<IdentityUserToken<int>>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });  // Khóa chính cho token
            entity.ToTable("UserTokens");  // Đặt tên bảng nếu muốn thay đổi
        });
        modelBuilder.Entity<IdentityUserClaim<int>>(entity =>
        {
            entity.HasKey(e => e.Id);  // Khóa chính là Id
            entity.ToTable("UserClaims");  // Đặt tên bảng nếu muốn thay đổi
        });
        // Cấu hình cho các thực thể khác

        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.AdminId).HasName("PK__Admins__43AA4141C99ADA52");

            entity.Property(e => e.AdminId).HasColumnName("admin_id");
            entity.Property(e => e.RoleDescription)
                .HasColumnType("text")
                .HasColumnName("role_description");
            entity.Property(e => e.UserId).HasColumnName("user_id");

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
                .IsUnicode(false)
                .HasColumnName("author");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Content)
                .HasColumnType("text")
                .HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.FeaturedImage)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("featured_image");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("updated_at");

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
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.LogoUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("logo_url");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsUnicode(false)
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
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.ParentId).HasColumnName("parent_id");

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("FK__Categorie__paren__5070F446");
        });

        modelBuilder.Entity<ChatConversation>(entity =>
        {
            entity.HasKey(e => e.ConversationId).HasName("PK__ChatConv__311E7E9ADE9B6CE2");

            entity.ToTable("ChatConversation");

            entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.LastMessageAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("last_message_at");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");
            entity.Property(e => e.StartedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("started_at");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Hoạt động")
                .HasColumnName("status");

            entity.HasOne(d => d.Customer).WithMany(p => p.ChatConversations)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__ChatConve__custo__1CBC4616");

            entity.HasOne(d => d.Staff).WithMany(p => p.ChatConversations)
                .HasForeignKey(d => d.StaffId)
                .HasConstraintName("FK__ChatConve__staff__1DB06A4F");
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__ChatMess__0BBF6EE6C7322F61");

            entity.ToTable("ChatMessage");

            entity.Property(e => e.MessageId).HasColumnName("message_id");
            entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
            entity.Property(e => e.MessageText)
                .HasColumnType("text")
                .HasColumnName("message_text");
            entity.Property(e => e.SenderId).HasColumnName("sender_id");
            entity.Property(e => e.SenderRole)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("sender_role");
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("sent_at");

            entity.HasOne(d => d.Conversation).WithMany(p => p.ChatMessages)
                .HasForeignKey(d => d.ConversationId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__ChatMessa__conve__2180FB33");
        });

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Coupons__3213E83F505ACA10");

            entity.HasIndex(e => e.Code, "UQ__Coupons__357D4CF99D5DF3F0").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.DiscountAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("discount_amount");
            entity.Property(e => e.DiscountPercentage)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("discount_percentage");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.MaxDiscountAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("max_discount_amount");
            entity.Property(e => e.MinimumOrderAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("minimum_order_amount");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.QuantityAvailable)
                .HasDefaultValue(0)
                .HasColumnName("quantity_available");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PK__Customer__CD65CB8580B29AEF");

            entity.ToTable(tb => tb.HasTrigger("trg_update_membership_level"));

            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.Address)
                .HasColumnType("text")
                .HasColumnName("address");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.MembershipLevelId).HasColumnName("membership_level_id");
            entity.Property(e => e.TotalSpending)
                .HasDefaultValue(0.00m)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("total_spending");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.MembershipLevel).WithMany(p => p.Customers)
                .HasForeignKey(d => d.MembershipLevelId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__Customers__membe__45F365D3");

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

            entity.ToTable("Inventory", tb => tb.HasTrigger("trg_notify_low_stock"));

            entity.Property(e => e.InventoryId).HasColumnName("inventory_id");
            entity.Property(e => e.LastUpdated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("last_updated");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.QuantityInStock).HasColumnName("quantity_in_stock");
            entity.Property(e => e.WarehouseLocation)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("warehouse_location");

            entity.HasOne(d => d.Product).WithMany(p => p.Inventories)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__Inventory__produ__6477ECF3");
        });

        modelBuilder.Entity<MembershipLevel>(entity =>
        {
            entity.HasKey(e => e.MembershipLevelId).HasName("PK__Membersh__0202741249F9C39C");

            entity.Property(e => e.MembershipLevelId).HasColumnName("membership_level_id");
            entity.Property(e => e.Benefits)
                .HasColumnType("text")
                .HasColumnName("benefits");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.LevelName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("level_name");
            entity.Property(e => e.MinimumSpending)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("minimum_spending");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Orders__3213E83F03E2C29E");

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

            entity.HasIndex(e => e.CustomerId, "idx_orders_customer_id");

            entity.HasIndex(e => e.OrderDate, "idx_orders_order_date");

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
            entity.Property(e => e.OrderNotes)
                .HasColumnType("text")
                .HasColumnName("order_notes");
            entity.Property(e => e.OriginalTotalAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("original_total_amount");
            entity.Property(e => e.PaymentMethodId).HasColumnName("payment_method_id");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("payment_status");
            entity.Property(e => e.ShippingAddress)
                .HasColumnType("text")
                .HasColumnName("shipping_address");
            entity.Property(e => e.ShippingCompanyId).HasColumnName("shipping_company_id");
            entity.Property(e => e.ShippingCost)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("shipping_cost");
            entity.Property(e => e.ShippingMethod)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("shipping_method");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("total_amount");

            entity.HasOne(d => d.Coupon).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CouponId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__Orders__coupon_i__7F2BE32F");

            entity.HasOne(d => d.Customer).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK__Orders__customer__7C4F7684");

            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.Orders)
                .HasForeignKey(d => d.PaymentMethodId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__Orders__payment___7D439ABD");

            entity.HasOne(d => d.ShippingCompany).WithMany(p => p.Orders)
                .HasForeignKey(d => d.ShippingCompanyId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__Orders__shipping__7E37BEF6");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__OrderDet__3213E83F5509C535");

            entity.HasIndex(e => e.OrderId, "idx_orderdetails_order_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.ProductVariation)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("product_variation");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.TotalPrice)
                .HasComputedColumnSql("([quantity]*[unit_price])", true)
                .HasColumnType("decimal(21, 2)")
                .HasColumnName("total_price");
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("unit_price");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__OrderDeta__order__02084FDA");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__OrderDeta__produ__02FC7413");
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PaymentM__3213E83FD39C27F1");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("image_url");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Products__3213E83FE376E46C");

            entity.HasIndex(e => e.Id, "idx_products_product_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AverageRating)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(3, 2)")
                .HasColumnName("average_rating");
            entity.Property(e => e.BrandId).HasColumnName("brand_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.FavoriteCount)
                .HasDefaultValue(0)
                .HasColumnName("favorite_count");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("image_url");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.OriginalPrice)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("original_price");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.ReviewCount)
                .HasDefaultValue(0)
                .HasColumnName("review_count");

            entity.HasOne(d => d.Brand).WithMany(p => p.Products)
                .HasForeignKey(d => d.BrandId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__Products__brand___5BE2A6F2");

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
            entity.HasKey(e => e.FeedbackId).HasName("PK__ProductF__7A6B2B8C9FF7CC51");

            entity.ToTable("ProductFeedback");

            entity.Property(e => e.FeedbackId).HasColumnName("feedback_id");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.FeedbackDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("feedback_date");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.ReviewText)
                .HasColumnType("text")
                .HasColumnName("review_text");

            entity.HasOne(d => d.Customer).WithMany(p => p.ProductFeedbacks)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK__ProductFe__custo__114A936A");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductFeedbacks)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__ProductFe__produ__123EB7A3");
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Promotio__3213E83FB4EFB36C");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.DiscountPercentage)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("discount_percentage");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.StartDate).HasColumnName("start_date");

            entity.HasOne(d => d.Product).WithMany(p => p.Promotions)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Promotion__produ__0C85DE4D");
        });

        modelBuilder.Entity<ShippingCompany>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Shipping__3213E83F51415519");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("image_url");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.ShippingCost)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("shipping_cost");
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasKey(e => e.StaffId).HasName("PK__Staff__1963DD9C4B3F26DF");

            entity.Property(e => e.StaffId).HasColumnName("staff_id");
            entity.Property(e => e.HireDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("hire_date");
            entity.Property(e => e.Position)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("position");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Staff)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Staff__user_id__4CA06362");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3213E83F9A29568F");

            entity.HasIndex(e => e.Email, "UQ__Users__AB6E6164E4DFD325").IsUnique();

            entity.HasIndex(e => e.Username, "UQ__Users__F3DBC57210DC9F7D").IsUnique();

            entity.HasIndex(e => e.Email, "idx_users_email");

            entity.HasIndex(e => e.Username, "idx_users_username");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("first_name");
            entity.Property(e => e.LastModified)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("last_modified");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("last_name");
            entity.Property(e => e.Password)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("password");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("role");
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("username");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
