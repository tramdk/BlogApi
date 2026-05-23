using FloraCore.Domain.ValueObjects;
using System;

namespace FloraCore.Application.Features.Orders.Queries;

public record OrderDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public DateTime OrderDate { get; init; }
    public Address ShippingAddress { get; init; } = null!;
    public string OrderStatus { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
}
