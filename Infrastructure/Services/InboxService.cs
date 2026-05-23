using FloraCore.Application.Common.Interfaces;
using FloraCore.Domain.Entities;
using FloraCore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FloraCore.Infrastructure.Services;

/// <summary>
/// Service implementing the Inbox Pattern to track message/event processing status.
/// </summary>
public class InboxService(AppDbContext context) : IInboxService
{
    private readonly AppDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <inheritdoc />
    public async Task<bool> HasBeenProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        return await _context.InboxMessages.AnyAsync(m => m.Id == messageId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task MarkAsProcessedAsync(Guid messageId, string eventName, CancellationToken cancellationToken = default)
    {
        var inboxMessage = new InboxMessage
        {
            Id = messageId,
            EventName = eventName,
            ReceivedOnUtc = DateTime.UtcNow,
            ProcessedOnUtc = DateTime.UtcNow
        };

        _context.InboxMessages.Add(inboxMessage);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
