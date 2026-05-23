
using FloraCore.Application.Common.Interfaces;
using FloraCore.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FloraCore.Application.Features.Orders.Queries;

/// <summary>
/// Handler for <see cref="GetOrderStatisticsQuery"/> to retrieve order statistics.
/// </summary>
public class GetOrderStatisticsQueryHandler(IGenericRepository<Order, Guid> orderRepository) 
    : IRequestHandler<GetOrderStatisticsQuery, OrderStatisticsDto>
{
    private readonly IGenericRepository<Order, Guid> _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));

    /// <inheritdoc />
    public async Task<OrderStatisticsDto> Handle(GetOrderStatisticsQuery request, CancellationToken cancellationToken)
    {
        var query = _orderRepository.GetQueryable().AsNoTracking();

        if (request.StartDate.HasValue)
        {
            query = query.Where(o => o.OrderDate >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(o => o.OrderDate <= request.EndDate.Value);
        }

        var orders = await query.ToListAsync(cancellationToken);

        if (!orders.Any())
        {
            return new OrderStatisticsDto
            {
                TotalOrders = 0,
                TotalRevenue = 0,
                AverageOrderValue = 0,
                OrdersByStatus = new Dictionary<string, int>(),
                RevenueByMonth = new Dictionary<string, decimal>()
            };
        }

        var totalOrders = orders.Count;
        var totalRevenue = orders.Sum(o => o.TotalAmount);
        var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

        var ordersByStatus = orders
            .GroupBy(o => o.OrderStatus)
            .ToDictionary(g => g.Key, g => g.Count());

        var revenueByMonth = orders
            .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
            .ToDictionary(
                g => $"{g.Key.Year}-{g.Key.Month:D2}",
                g => g.Sum(o => o.TotalAmount)
            );

        return new OrderStatisticsDto
        {
            TotalOrders = totalOrders,
            TotalRevenue = totalRevenue,
            AverageOrderValue = averageOrderValue,
            OrdersByStatus = ordersByStatus,
            RevenueByMonth = revenueByMonth
        };
    }
}
