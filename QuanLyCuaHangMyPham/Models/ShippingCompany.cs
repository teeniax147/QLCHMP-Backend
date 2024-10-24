using System;
using System.Collections.Generic;

namespace QuanLyCuaHangMyPham.Models;

public partial class ShippingCompany
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public decimal? ShippingCost { get; set; }

    public string? ImageUrl { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
