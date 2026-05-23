using FloraCore.Application.Common.Interfaces;
using FloraCore.Domain.Entities;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FloraCore.Application.Interfaces;

namespace FloraCore.Application.Features.Orders.Commands;

public record AddOrderItemCommand(Guid OrderId, Guid ProductId, int Quantity, decimal Price) : IRequest<bool>;

public class AddOrderItemCommandHandler : IRequestHandler<AddOrderItemCommand, bool>
{
    private readonly IOrderRepository _repository;
    private readonly IGenericRepository<Product, Guid> _productRepository;

    public AddOrderItemCommandHandler(IOrderRepository repository, IGenericRepository<Product, Guid> productRepository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(productRepository);
        _repository = repository;
        _productRepository = productRepository;
    }

    public async Task<bool> Handle(AddOrderItemCommand request, CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(request.OrderId);
        if (order == null) return false;

        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product == null) return false;

        var orderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = request.OrderId,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            Price = request.Price
        };

        order.TotalAmount += request.Price * request.Quantity;
        await _repository.UpdateAsync(order);
        await _repository.AddOrderItemAsync(orderItem);
        return true;
    }
}
