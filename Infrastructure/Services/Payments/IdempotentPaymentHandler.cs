using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FloraCore.Application.Common.Interfaces;
using FloraCore.Application.Common.Models;
using FloraCore.Domain.Constants;
using FloraCore.Domain.Entities;
using FloraCore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FloraCore.Infrastructure.Services.Payments;

/// <summary>
/// Processes payment gateway callbacks with inbox-pattern idempotency to prevent duplicate order updates.
/// </summary>
public class IdempotentPaymentHandler(
    AppDbContext context,
    IPaymentServiceFactory paymentServiceFactory,
    ILogger<IdempotentPaymentHandler> logger) : IIdempotentPaymentHandler
{
    private readonly AppDbContext _context =
        context ?? throw new ArgumentNullException(nameof(context));
    private readonly IPaymentServiceFactory _paymentServiceFactory =
        paymentServiceFactory ?? throw new ArgumentNullException(nameof(paymentServiceFactory));
    private readonly ILogger<IdempotentPaymentHandler> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<bool> ProcessPaymentCallbackAsync(string gateway, string transactionId, PaymentCallbackDto callbackData)
    {
        var service = _paymentServiceFactory.GetPaymentService(gateway);
        var isVerified = await service.VerifyCallbackAsync(callbackData);
        if (!isVerified)
        {
            _logger.LogWarning("Payment verification failed for gateway {Gateway}, txn {TxnId}", gateway, transactionId);
            return false;
        }

        // Generate deterministic Guid for Inbox idempotency
        var uniqueKey = $"{gateway}_{transactionId}".ToLowerInvariant();
        Guid inboxMessageId;
        using (var md5 = MD5.Create())
        {
            var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(uniqueKey));
            inboxMessageId = new Guid(hashBytes);
        }

        // Use database transaction to guarantee consistency
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Check if already processed
            var exists = await _context.InboxMessages.AnyAsync(i => i.Id == inboxMessageId);
            if (exists)
            {
                _logger.LogInformation("Payment callback already processed for {Gateway}, txn {TxnId} (Idempotent call)", gateway, transactionId);
                return true; 
            }

            // Find order
            Order? order = null;
            if (string.Equals(gateway, PaymentMethod.PAYOS, StringComparison.OrdinalIgnoreCase))
            {
                if (long.TryParse(transactionId, out var payOsCode))
                {
                    var orders = await _context.Orders.ToListAsync();
                    order = orders.FirstOrDefault(o => 
                        Math.Abs(BitConverter.ToInt64(o.Id.ToByteArray(), 0) % 9999999999) == payOsCode);
                }
            }
            else
            {
                if (Guid.TryParse(transactionId, out var orderGuid))
                {
                    order = await _context.Orders.FindAsync(orderGuid);
                }
            }

            if (order == null)
            {
                _logger.LogWarning("Order not found for transaction {TxnId} under gateway {Gateway}", transactionId, gateway);
                return false;
            }

            // Save Inbox message
            var inbox = new InboxMessage
            {
                Id = inboxMessageId,
                EventName = $"PaymentCallback_{gateway}",
                ReceivedOnUtc = DateTime.UtcNow,
                ProcessedOnUtc = DateTime.UtcNow,
                Gateway = gateway,
                Payload = JsonSerializer.Serialize(callbackData)
            };
            _context.InboxMessages.Add(inbox);

            // Save Payment transaction audit
            var tx = new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                TransactionId = transactionId,
                PaymentGateway = gateway,
                Amount = order.TotalAmount,
                Status = PaymentStatus.Paid,
                CreatedAt = DateTime.UtcNow,
                ResponseMessage = $"Payment verified successfully via {gateway} callback."
            };
            _context.PaymentTransactions.Add(tx);

            // Create order history before status changes
            var history = new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                FromStatus = order.OrderStatus,
                ToStatus = OrderStatus.Processing,
                ChangedAt = DateTime.UtcNow
            };
            _context.OrderStatusHistories.Add(history);

            // Update order status
            order.PaymentStatus = PaymentStatus.Paid;
            order.OrderStatus = OrderStatus.Processing;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Order {OrderId} successfully paid via {Gateway}", order.Id, gateway);
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error processing payment callback for {Gateway}, txn {TxnId}", gateway, transactionId);
            return false;
        }
    }
}
