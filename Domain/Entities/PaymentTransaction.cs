using System;

namespace FloraCore.Domain.Entities;

public class PaymentTransaction
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public string TransactionId { get; set; } = string.Empty;
    public string PaymentGateway { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string ResponseMessage { get; set; } = string.Empty;
}
