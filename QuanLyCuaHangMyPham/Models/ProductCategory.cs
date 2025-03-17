using QuanLyCuaHangMyPham.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class ProductCategory
{
    public int ProductId { get; set; }
    public int CategoryId { get; set; }

    public virtual Product Product { get; set; } = null!;
    public virtual Category Category { get; set; } = null!;
}
