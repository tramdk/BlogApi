using FloraCore.Application.Common.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FloraCore.Application.Interfaces;

namespace FloraCore.Application.Features.Orders.Queries;

public record GetOrdersQuery : IRequest<List<OrderDto>>;

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, List<OrderDto>>
{
    private readonly IOrderRepository _repository;

    public GetOrdersQueryHandler(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await _repository.GetAllAsync();
        return orders.Select(order => new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            OrderDate = order.OrderDate,
            ShippingAddress = order.ShippingAddress,
            OrderStatus = order.OrderStatus,
            TotalAmount = order.TotalAmount
        }).ToList();
    }
}
