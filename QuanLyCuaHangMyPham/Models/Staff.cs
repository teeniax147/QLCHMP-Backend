using QuanLyCuaHangMyPham.IdentityModels;
using System;
using System.Collections.Generic;

namespace QuanLyCuaHangMyPham.Models;

public partial class Staff
{
    public int StaffId { get; set; }

    public int UserId { get; set; }

    public string? Position { get; set; }

    public DateTime? HireDate { get; set; }

    public virtual ApplicationUser User { get; set; } = null!;
}
