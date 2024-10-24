using System;
using System.Collections.Generic;

namespace QuanLyCuaHangMyPham.Models;

public partial class Cart
{
    public int CartId { get; set; }

    public int CustomerId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? LastUpdated { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual Customer Customer { get; set; } = null!;
}
