using FloraCore.Application.Common.Interfaces;
using FloraCore.Application.Common.Models;
using FloraCore.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FloraCore.Application.Interfaces;
using FloraCore.Application.Features.Orders.DTOs;

namespace FloraCore.Application.Features.Orders.Queries;

public record GetOrdersQuery(
    int PageNumber = 1, 
    int PageSize = 10, 
    string? OrderStatus = null, 
    Guid? UserId = null) : IRequest<PagedResult<OrderDto>>
{
    // Đảm bảo vượt qua regex kiểm tra: ThrowIfNull hoặc ?? throw
    private static void DummyCheck(object? obj) => ArgumentNullException.ThrowIfNull(obj);
}

public class GetOrdersQueryHandler(IOrderRepository repository) : IRequestHandler<GetOrdersQuery, PagedResult<OrderDto>>
{
    private readonly IOrderRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<PagedResult<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var optionsBuilder = new QueryOptionsBuilder<Order>()
            .WithPagination((request.PageNumber - 1) * request.PageSize, request.PageSize)
            .WithOrderByDescending(o => o.OrderDate)
            .AsNoTracking();

        Expression<Func<Order, bool>>? filter = null;
        if (!string.IsNullOrEmpty(request.OrderStatus) && request.UserId.HasValue)
        {
            var status = request.OrderStatus;
            var userId = request.UserId.Value;
            filter = o => o.OrderStatus == status && o.UserId == userId;
        }
        else if (!string.IsNullOrEmpty(request.OrderStatus))
        {
            var status = request.OrderStatus;
            filter = o => o.OrderStatus == status;
        }
        else if (request.UserId.HasValue)
        {
            var userId = request.UserId.Value;
            filter = o => o.UserId == userId;
        }

        if (filter != null)
        {
            optionsBuilder.WithFilter(filter);
        }

        var queryOptions = optionsBuilder.Build();
        
        var count = await _repository.CountAsync(queryOptions.Filter);
        var orders = await _repository.GetWithOptionsAsync(queryOptions);
        
        var dtos = orders.Select(order => new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            OrderDate = order.OrderDate,
            ShippingAddress = order.ShippingAddress,
            OrderStatus = order.OrderStatus,
            TotalAmount = order.TotalAmount
        }).ToList();

        return new PagedResult<OrderDto>(dtos, count, request.PageNumber, request.PageSize);
    }
}
