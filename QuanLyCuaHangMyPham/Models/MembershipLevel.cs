using System;
using System.Collections.Generic;

namespace QuanLyCuaHangMyPham.Models;

public partial class MembershipLevel
{
    public int MembershipLevelId { get; set; }

    public string LevelName { get; set; } = null!;

    public decimal MinimumSpending { get; set; }

    public string? Benefits { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
