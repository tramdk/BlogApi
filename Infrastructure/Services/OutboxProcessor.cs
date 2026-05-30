using System.Text.Json;
using FloraCore.Application.Common.Interfaces;
using FloraCore.Domain.Entities;
using FloraCore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FloraCore.Application.Common.Models;

namespace FloraCore.Infrastructure.Services;

/// <summary>
/// Service to process messages in the Outbox table.
/// </summary>
public class OutboxProcessor(
    AppDbContext context, 
    INotificationService notificationService, 
    IPaymentServiceFactory paymentServiceFactory,
    ILogger<OutboxProcessor> logger)
{
    private readonly AppDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly INotificationService _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    private readonly IPaymentServiceFactory _paymentServiceFactory = paymentServiceFactory ?? throw new ArgumentNullException(nameof(paymentServiceFactory));
    private readonly ILogger<OutboxProcessor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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
                else if (message.Type == "OrderCreatedPaymentEvent")
                {
                    var orderPaymentDto = JsonSerializer.Deserialize<OrderPaymentDto>(message.Content);
                    if (orderPaymentDto != null)
                    {
                        var order = await _context.Orders.FindAsync(orderPaymentDto.OrderId);
                        if (order != null)
                        {
                            var service = _paymentServiceFactory.GetPaymentService(order.PaymentMethod);
                            var result = await service.CreatePaymentUrlAsync(orderPaymentDto);
                            if (result.Success)
                            {
                                order.PaymentUrl = result.PaymentUrl;
                            }
                            else
                            {
                                throw new Exception($"Payment gateway failed: {result.Message}");
                            }
                        }
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
