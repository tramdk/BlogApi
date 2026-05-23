using System;
using System.Threading;
using System.Threading.Tasks;

namespace FloraCore.Application.Common.Interfaces;

/// <summary>
/// Contract for the Inbox Pattern to prevent duplicate processing of event/message.
/// </summary>
public interface IInboxService
{
    /// <summary>
    /// Checks if the message/event has already been processed.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if it has been processed; otherwise, false.</returns>
    Task<bool> HasBeenProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the message/event as processed.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="eventName">The name of the event.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task MarkAsProcessedAsync(Guid messageId, string eventName, CancellationToken cancellationToken = default);
}
