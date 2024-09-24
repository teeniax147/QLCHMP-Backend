using System;
using System.Collections.Generic;

namespace QuanLyCuaHangMyPham.Models;

public partial class Review
{
    public int ReviewId { get; set; }

    public int? CustomerId { get; set; }

    public int? ProductId { get; set; }

    public string? ReviewText { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Product? Product { get; set; }
}
