using System;
using System.Collections.Generic;

namespace QuanLyCuaHangMyPham.Models;

public partial class Admin
{
    public int AdminId { get; set; }

    public int UserId { get; set; }

    public string? RoleDescription { get; set; }

    public virtual User User { get; set; } = null!;
}
