using System;
using System.Collections.Generic;

namespace QuanLyCuaHangMyPham.Models;

public partial class Coupon
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public decimal? DiscountAmount { get; set; }

    public decimal? DiscountPercentage { get; set; }

    public decimal? MaxDiscountAmount { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public decimal? MinimumOrderAmount { get; set; }

    public int? QuantityAvailable { get; set; }

    public DateTime? CreatedAt { get; set; }
    private bool? _isActive;
    // Tính toán giá trị IsActive dựa trên StartDate và EndDate
    public bool IsActive
    {
        get
        {
            if (StartDate.HasValue && EndDate.HasValue)
            {
                var currentDate = DateTime.Now.Date;
                return currentDate >= StartDate.Value.ToDateTime(new TimeOnly(0, 0)) &&
                       currentDate <= EndDate.Value.ToDateTime(new TimeOnly(23, 59));
            }
            return false; // Nếu không có StartDate hoặc EndDate thì tự động set IsActive là false
        }
        set
        {
            _isActive = value;
        }
    }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
