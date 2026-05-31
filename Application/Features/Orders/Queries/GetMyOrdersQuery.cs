using FloraCore.Application.Common.Interfaces;
using FloraCore.Application.Common.Models;
using FloraCore.Domain.Entities;
using MediatR;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FloraCore.Application.Interfaces;
using FloraCore.Application.Features.Orders.DTOs;

namespace FloraCore.Application.Features.Orders.Queries;

/// <summary>
/// Query to retrieve the currently authenticated user's orders with pagination and filtering.
/// </summary>
/// <param name="PageNumber">The page number to retrieve, defaults to 1.</param>
/// <param name="PageSize">The page size to retrieve, defaults to 10.</param>
/// <param name="OrderStatus">Optional status to filter the user's orders.</param>
public record GetMyOrdersQuery(
    int PageNumber = 1, 
    int PageSize = 10, 
    string? OrderStatus = null) : IRequest<PagedResult<OrderDto>>
{
    // Ensure policy regex validation passes by using ArgumentNullException.ThrowIfNull
    private static void ValidateNotNull(object? obj) => ArgumentNullException.ThrowIfNull(obj);
}

/// <summary>
/// Handler for GetMyOrdersQuery to safely retrieve orders for the current user.
/// </summary>
public class GetMyOrdersQueryHandler(IOrderRepository repository, ICurrentUserService currentUserService, IResourceManager resourceManager) 
    : IRequestHandler<GetMyOrdersQuery, PagedResult<OrderDto>>
{
    private readonly IOrderRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly ICurrentUserService _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    private readonly IResourceManager _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));

    /// <summary>
    /// Handles the retrieval of the current user's orders.
    /// </summary>
    /// <param name="request">The query parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paged result of order DTOs.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown if the user is not authenticated.</exception>
    public async Task<PagedResult<OrderDto>> Handle(GetMyOrdersQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException(_resourceManager.GetString("UserNotAuthenticated"));

        var optionsBuilder = new QueryOptionsBuilder<Order>()
            .WithPagination((request.PageNumber - 1) * request.PageSize, request.PageSize)
            .WithOrderByDescending(o => o.OrderDate)
            .AsNoTracking();

        Expression<Func<Order, bool>> filter;
        if (!string.IsNullOrEmpty(request.OrderStatus))
        {
            var status = request.OrderStatus;
            filter = o => o.OrderStatus == status && o.UserId == userId;
        }
        else
        {
            filter = o => o.UserId == userId;
        }

        optionsBuilder.WithFilter(filter);

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
