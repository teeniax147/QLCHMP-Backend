using System;
using System.Collections.Generic;

namespace QuanLyCuaHangMyPham.Models;

public partial class Customer
{
    public int Id { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string? Address { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? MembershipLevelId { get; set; }

    public decimal? TotalSpending { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public byte[]? RowVersion { get; set; }

    public bool IsVerified { get; set; }

    public bool IsSuspended { get; set; }

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual ICollection<ChatConversation> ChatConversations { get; set; } = new List<ChatConversation>();

    public virtual ICollection<CustomerShippingAddress> CustomerShippingAddresses { get; set; } = new List<CustomerShippingAddress>();

    public virtual MembershipLevel? MembershipLevel { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Rate> Rates { get; set; } = new List<Rate>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
