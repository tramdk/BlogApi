using FloraCore.Application.Common.Interfaces;
using FloraCore.Domain.ValueObjects;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using FloraCore.Application.Interfaces;
using FloraCore.Domain.Entities;

namespace FloraCore.Application.Features.Orders.Commands;

/// <summary>
/// Command to update general order details (Shipping address, receiver, payment method).
/// </summary>
public record UpdateOrderDetailsCommand(
    Guid Id, 
    Address ShippingAddress, 
    string PaymentMethod) : IRequest<bool>;

/// <summary>
/// Handler for UpdateOrderDetailsCommand.
/// </summary>
public class UpdateOrderDetailsCommandHandler(
    IOrderRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateOrderDetailsCommand, bool>
{
    private readonly IOrderRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    public async Task<bool> Handle(UpdateOrderDetailsCommand request, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(request.Id);
        if (order == null) return false;

        // Smart detect: only allowed to edit if order status is Pending and payment status is Pending
        if (order.OrderStatus != Domain.Constants.OrderStatus.Pending || order.PaymentStatus != Domain.Constants.PaymentStatus.Pending)
        {
            return false;
        }

        // Update shipping address
        order.ShippingAddress = request.ShippingAddress;

        // Detect payment method changes
        if (request.PaymentMethod != order.PaymentMethod)
        {
            order.PaymentMethod = request.PaymentMethod;
            order.PaymentUrl = null; // Clear old payment link immediately
        }

        _repository.StageUpdate(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
