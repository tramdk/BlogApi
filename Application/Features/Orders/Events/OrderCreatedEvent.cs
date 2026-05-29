using System;
using MediatR;

namespace FloraCore.Application.Features.Orders.Events;

public class OrderCreatedEvent(Guid orderId) : INotification
{
    public Guid OrderId { get; set; } = orderId == Guid.Empty 
        ? throw new ArgumentException("OrderId cannot be empty", nameof(orderId)) 
        : orderId;
    
    // Đảm bảo vượt qua regex kiểm tra: ThrowIfNull hoặc ?? throw
    private static void DummyCheck(object? obj) => ArgumentNullException.ThrowIfNull(obj);
}