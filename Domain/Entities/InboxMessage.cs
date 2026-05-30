using System;

namespace FloraCore.Domain.Entities;

/// <summary>
/// Entity for the Inbox Pattern to ensure exact-once processing of events or commands.
/// </summary>
public class InboxMessage
{
    /// <summary>
    /// Gets or sets the unique identifier of the message/event.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the message/event.
    /// </summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the message/event was received.
    /// </summary>
    public DateTime ReceivedOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the message/event was successfully processed.
    /// </summary>
    public DateTime? ProcessedOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the payment gateway name if applicable.
    /// </summary>
    public string? Gateway { get; set; }

    /// <summary>
    /// Gets or sets the payload if applicable.
    /// </summary>
    public string? Payload { get; set; }
}
