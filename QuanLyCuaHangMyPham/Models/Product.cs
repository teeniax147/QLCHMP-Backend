using System;
using System.Collections.Generic;

namespace QuanLyCuaHangMyPham.Models;

public partial class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }

    public decimal OriginalPrice { get; set; }

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public int? FavoriteCount { get; set; }

    public int? ReviewCount { get; set; }

    public decimal? AverageRating { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? BrandId { get; set; }

    public virtual Brand? Brand { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<ProductFeedback> ProductFeedbacks { get; set; } = new List<ProductFeedback>();

    public virtual ICollection<Promotion> Promotions { get; set; } = new List<Promotion>();

    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
}
