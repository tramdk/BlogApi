using FloraCore.Application.Common.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using FloraCore.Application.Interfaces;

namespace FloraCore.Application.Features.Orders.Commands;

public record UpdateOrderCommand(Guid Id, string OrderStatus) : IRequest<bool>;

public class UpdateOrderCommandHandler : IRequestHandler<UpdateOrderCommand, bool>
{
    private readonly IOrderRepository _repository;

    public UpdateOrderCommandHandler(IOrderRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public async Task<bool> Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(request.Id);
        if (order == null) return false;

        order.OrderStatus = request.OrderStatus;
        await _repository.UpdateAsync(order);
        return true;
    }
}
