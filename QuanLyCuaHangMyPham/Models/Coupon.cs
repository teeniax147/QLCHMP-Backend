using System;
using System.Collections.Generic;

namespace QuanLyCuaHangMyPham.Models;

public partial class Coupon
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public decimal? DiscountAmount { get; set; }

    public decimal? DiscountPercentage { get; set; }

    public decimal? MaxDiscountAmount { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public decimal? MinimumOrderAmount { get; set; }

    public int? QuantityAvailable { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
