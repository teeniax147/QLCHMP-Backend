using System;
using System.Collections.Generic;

namespace QuanLyCuaHangMyPham.Models;

public partial class CustomerShippingAddress
{
    public int AddressId { get; set; }

    public int? CustomerId { get; set; }

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? PostalCode { get; set; }

    public string? Country { get; set; }

    public string? PhoneNumber { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Customer? Customer { get; set; }
}
