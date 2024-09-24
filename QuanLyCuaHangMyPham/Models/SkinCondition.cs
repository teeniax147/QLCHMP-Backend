using System;
using System.Collections.Generic;

namespace QuanLyCuaHangMyPham.Models;

public partial class SkinCondition
{
    public int Id { get; set; }

    public string ConditionName { get; set; } = null!;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
