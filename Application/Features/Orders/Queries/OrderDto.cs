using FloraCore.Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace FloraCore.Application.Features.Orders.Queries;

/// <summary>
/// Data transfer object for order status history.
/// </summary>
public record OrderStatusHistoryDto
{
    /// <summary>
    /// Gets the unique identifier for the history record.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the order identifier.
    /// </summary>
    public Guid OrderId { get; init; }

    /// <summary>
    /// Gets the previous status of the order.
    /// </summary>
    public string FromStatus { get; init; } = string.Empty;

    /// <summary>
    /// Gets the new status of the order.
    /// </summary>
    public string ToStatus { get; init; } = string.Empty;

    /// <summary>
    /// Gets the date and time when the status was changed.
    /// </summary>
    public DateTime ChangedAt { get; init; }
}

/// <summary>
/// Data transfer object for Order.
/// </summary>
public record OrderDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public DateTime OrderDate { get; init; }
    public Address ShippingAddress { get; init; } = null!;
    public string OrderStatus { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }

    /// <summary>
    /// Gets the history of status changes for this order.
    /// </summary>
    public IReadOnlyCollection<OrderStatusHistoryDto> StatusHistories { get; init; } = Array.Empty<OrderStatusHistoryDto>();
}
