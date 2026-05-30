using FloraCore.Application.Common.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using FloraCore.Application.Interfaces;
using FloraCore.Domain.Entities;

namespace FloraCore.Application.Features.Orders.Commands;

/// <summary>
/// Command to update order status.
/// </summary>
public record UpdateOrderCommand(Guid Id, string OrderStatus) : IRequest<bool>;

/// <summary>
/// Handler for UpdateOrderCommand.
/// </summary>
public class UpdateOrderCommandHandler(IOrderRepository repository) : IRequestHandler<UpdateOrderCommand, bool>
{
    private readonly IOrderRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    /// <inheritdoc />
    public async Task<bool> Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(request.Id);
        if (order == null) return false;

        if (order.OrderStatus != request.OrderStatus)
        {
            order.StatusHistories.Add(new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                FromStatus = order.OrderStatus,
                ToStatus = request.OrderStatus,
                ChangedAt = DateTime.UtcNow
            });

            order.OrderStatus = request.OrderStatus;
            await _repository.UpdateAsync(order);
        }

        return true;
    }
}
