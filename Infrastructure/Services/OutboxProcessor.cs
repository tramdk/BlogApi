using System.Text.Json;
using BlogApi.Application.Common.Interfaces;
using BlogApi.Domain.Entities;
using BlogApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlogApi.Infrastructure.Services;

/// <summary>
/// Service to process messages in the Outbox table.
/// </summary>
public class OutboxProcessor
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(AppDbContext context, INotificationService notificationService, ILogger<OutboxProcessor> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task ProcessMessagesAsync()
    {
        var messages = await _context.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null && m.RetryCount < 5)
            .Take(20)
            .ToListAsync();

        foreach (var message in messages)
        {
            try
            {
                if (message.Type == "Notification")
                {
                    var notification = JsonSerializer.Deserialize<Notification>(message.Content);
                    if (notification != null)
                    {
                        await _notificationService.SendNotificationToUser(
                            notification.UserId, 
                            "Hệ thống", 
                            notification.Message, 
                            notification.Type, 
                            notification.RelatedId);
                    }
                }

                message.ProcessedOnUtc = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox message {MessageId}", message.Id);
                message.Error = ex.Message;
                message.RetryCount++;
            }
        }

        await _context.SaveChangesAsync();
    }
}
