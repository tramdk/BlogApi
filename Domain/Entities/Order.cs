using System;
using System.Collections.Generic;
using FloraCore.Domain.ValueObjects;

namespace FloraCore.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public DateTime OrderDate { get; set; }
    public Address ShippingAddress { get; set; } = null!;
    public string OrderStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
