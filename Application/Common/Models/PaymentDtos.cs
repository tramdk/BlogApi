using System;
using System.Collections.Generic;

namespace FloraCore.Application.Common.Models;

public class OrderPaymentDto
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
}

public class CreatePaymentResult
{
    public bool Success { get; set; }
    public string PaymentUrl { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class PaymentCallbackDto
{
    public Dictionary<string, string> QueryParameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string RawBody { get; set; } = string.Empty;
}
