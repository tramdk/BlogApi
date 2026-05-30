using FloraCore.Application.Common.Interfaces;
using FloraCore.Domain.Entities;
using FloraCore.Domain.ValueObjects;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using FloraCore.Application.Interfaces;
using FloraCore.Application.Common.Models;
using System.Linq;

namespace FloraCore.Application.Features.Orders.Queries;

/// <summary>
/// Query to get order by its identifier.
/// </summary>
public record GetOrderByIdQuery(Guid Id) : IRequest<OrderDto?>;

/// <summary>
/// Handler for GetOrderByIdQuery.
/// </summary>
public class GetOrderByIdQueryHandler(IOrderRepository repository) : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    private readonly IOrderRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    /// <inheritdoc />
    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var options = new QueryOptionsBuilder<Order>()
            .WithFilter(o => o.Id == request.Id)
            .WithInclude(o => o.StatusHistories)
            .AsNoTracking()
            .Build();

        var order = await _repository.GetSingleWithOptionsAsync(options);
        if (order == null) return null;

        return new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            OrderDate = order.OrderDate,
            ShippingAddress = order.ShippingAddress,
            OrderStatus = order.OrderStatus,
            TotalAmount = order.TotalAmount,
            StatusHistories = order.StatusHistories
                .Select(osh => new OrderStatusHistoryDto
                {
                    Id = osh.Id,
                    OrderId = osh.OrderId,
                    FromStatus = osh.FromStatus,
                    ToStatus = osh.ToStatus,
                    ChangedAt = osh.ChangedAt
                })
                .OrderBy(osh => osh.ChangedAt)
                .ToList()
        };
    }
}
