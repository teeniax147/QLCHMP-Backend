using QuanLyCuaHangMyPham.IdentityModels;
using System;
using System.Collections.Generic;

namespace QuanLyCuaHangMyPham.Models;

public partial class Customer
{
    public int CustomerId { get; set; }

    public int UserId { get; set; }

    public string? Address { get; set; }

    public decimal? TotalSpending { get; set; }

    public int? MembershipLevelId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual MembershipLevel? MembershipLevel { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<ProductFeedback> ProductFeedbacks { get; set; } = new List<ProductFeedback>();

    public virtual ApplicationUser User { get; set; } = null!;
}
