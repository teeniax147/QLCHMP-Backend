using System;
using System.Collections.Generic;

namespace QuanLyCuaHangMyPham.Models;

public partial class ProductFeedback
{
    public int FeedbackId { get; set; }

    public int CustomerId { get; set; }

    public int ProductId { get; set; }

    public int? Rating { get; set; }

    public string? ReviewText { get; set; }

    public DateTime? FeedbackDate { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
