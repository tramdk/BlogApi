
using MediatR;
using FloraCore.Application.Features.Orders.DTOs;
using System;

namespace FloraCore.Application.Features.Orders.Queries;

/// <summary>
/// Query to get order statistics.
/// </summary>
public record GetOrderStatisticsQuery : IRequest<OrderStatisticsDto>
{
    /// <summary>
    /// Optional start date to filter orders.
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// Optional end date to filter orders.
    /// </summary>
    public DateTime? EndDate { get; init; }
}
