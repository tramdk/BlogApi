using FloraCore.Application.Common.Interfaces;
using FloraCore.Domain.Entities;
using FloraCore.Domain.ValueObjects;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using FloraCore.Application.Interfaces;

namespace FloraCore.Application.Features.Orders.Commands;

public record CreateOrderCommand(Guid UserId, Address ShippingAddress) : IRequest<Guid>;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IOrderRepository _repository;

    public CreateOrderCommandHandler(IOrderRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            OrderDate = DateTime.UtcNow,
            ShippingAddress = request.ShippingAddress,
            OrderStatus = Domain.Constants.OrderStatus.Pending,
            TotalAmount = 0,
            OrderItems = new List<OrderItem>()
        };

        await _repository.AddAsync(order);
        return order.Id;
    }
}
