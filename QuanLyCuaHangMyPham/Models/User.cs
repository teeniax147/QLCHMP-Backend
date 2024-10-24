using System;
using System.Collections.Generic;

namespace QuanLyCuaHangMyPham.Models;

public partial class User
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public string Role { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public bool IsVerified { get; set; }

    public bool IsSuspended { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? LastModified { get; set; }

    public virtual ICollection<Admin> Admins { get; set; } = new List<Admin>();

    public virtual ICollection<ChatConversation> ChatConversations { get; set; } = new List<ChatConversation>();

    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();

    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    public virtual ICollection<Staff> Staff { get; set; } = new List<Staff>();
}
