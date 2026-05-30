using System;

namespace FloraCore.Domain.Entities;

/// <summary>
/// Represents the history of order status changes.
/// </summary>
public class OrderStatusHistory
{
    /// <summary>
    /// Gets or sets the unique identifier for the history record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the order identifier.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Gets or sets the related order.
    /// </summary>
    public Order Order { get; set; } = null!;

    /// <summary>
    /// Gets or sets the previous status of the order.
    /// </summary>
    public string FromStatus { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new status of the order.
    /// </summary>
    public string ToStatus { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the status was changed.
    /// </summary>
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
