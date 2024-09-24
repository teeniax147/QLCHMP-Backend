using System;
using System.Collections.Generic;

namespace QuanLyCuaHangMyPham.Models;

public partial class BeautyBlog
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string? Author { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? FeaturedImage { get; set; }

    public int? CategoryId { get; set; }

    public virtual Category? Category { get; set; }
}
