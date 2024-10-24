using System;
using System.Collections.Generic;

namespace QuanLyCuaHangMyPham.Models;

public partial class Rate
{
    public int RateId { get; set; }

    public int CustomerId { get; set; }

    public int ProductId { get; set; }

    public int? Rating { get; set; }

    public DateTime? RatedAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
