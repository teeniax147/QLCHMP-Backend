using System;
using System.Collections.Generic;

namespace QuanLyCuaHangMyPham.Models;

public partial class Order
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public int? CouponId { get; set; }

    public int? PaymentMethodId { get; set; }

    public decimal? OriginalTotalAmount { get; set; }

    public decimal? TotalAmount { get; set; }

    public string? ShippingAddress { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public int? ShippingCompanyId { get; set; }

    public string? ShippingMethod { get; set; }

    public decimal? ShippingCost { get; set; }

    public decimal? DiscountApplied { get; set; }

    public string? OrderNotes { get; set; }

    public string? PaymentStatus { get; set; }

    public string? Status { get; set; }

    public DateTime? OrderDate { get; set; }

    public DateTime? EstimatedDeliveryDate { get; set; }

    public virtual Coupon? Coupon { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual PaymentMethod? PaymentMethod { get; set; }

    public virtual ShippingCompany? ShippingCompany { get; set; }
}
