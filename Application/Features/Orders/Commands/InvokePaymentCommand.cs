using FloraCore.Application.Common.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using FloraCore.Application.Interfaces;
using FloraCore.Application.Common.Models;

namespace FloraCore.Application.Features.Orders.Commands;

/// <summary>
/// Command to invoke a payment session for an existing order and generate a fresh Payment Url.
/// </summary>
public record InvokePaymentCommand(Guid OrderId, string ApiUrl) : IRequest<CreatePaymentResult>;

/// <summary>
/// Handler for InvokePaymentCommand.
/// </summary>
public class InvokePaymentCommandHandler(
    IOrderRepository repository,
    IPaymentServiceFactory paymentServiceFactory,
    IUnitOfWork unitOfWork) : IRequestHandler<InvokePaymentCommand, CreatePaymentResult>
{
    private readonly IOrderRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly IPaymentServiceFactory _paymentServiceFactory = paymentServiceFactory ?? throw new ArgumentNullException(nameof(paymentServiceFactory));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    public async Task<CreatePaymentResult> Handle(InvokePaymentCommand request, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(request.OrderId);
        if (order == null)
        {
            return new CreatePaymentResult { Success = false, Message = "Order not found." };
        }

        if (order.PaymentStatus != Domain.Constants.PaymentStatus.Pending || order.OrderStatus != Domain.Constants.OrderStatus.Pending)
        {
            return new CreatePaymentResult { Success = false, Message = "Order is not in pending state or already paid." };
        }

        if (order.PaymentMethod == "COD")
        {
            return new CreatePaymentResult { Success = false, Message = "COD payment method does not support online payment links." };
        }

        // Get payment service according to the updated payment method
        var paymentService = _paymentServiceFactory.GetPaymentService(order.PaymentMethod);
        
        var returnUrl = order.PaymentMethod switch
        {
            "VNPAY" => $"{request.ApiUrl}/api/v1/payments/vnpay-callback",
            "MOMO" => $"{request.ApiUrl}/api/v1/payments/momo-ipn", // Or a dedicated frontend redirect url
            "PAYOS" => $"{request.ApiUrl}/api/v1/payments/payos-webhook",
            _ => $"{request.ApiUrl}/api/v1/payments/vnpay-callback"
        };

        var orderPaymentDto = new OrderPaymentDto
        {
            OrderId = order.Id,
            Amount = order.TotalAmount,
            Description = $"Thanh toan don hang {order.Id}",
            ReturnUrl = returnUrl
        };

        var result = await paymentService.CreatePaymentUrlAsync(orderPaymentDto);
        if (result.Success && !string.IsNullOrEmpty(result.PaymentUrl))
        {
            order.PaymentUrl = result.PaymentUrl;
            _repository.StageUpdate(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return result;
    }
}
