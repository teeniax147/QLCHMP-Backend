using System;
using System.Collections.Generic;

namespace QuanLyCuaHangMyPham.Models;

public partial class Promotion
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public decimal? DiscountPercentage { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public DateTime? CreatedAt { get; set; }
}
