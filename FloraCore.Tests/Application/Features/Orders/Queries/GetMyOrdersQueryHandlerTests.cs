using FloraCore.Application.Common.Interfaces;
using FloraCore.Application.Common.Models;
using FloraCore.Application.Features.Orders.Queries;
using FloraCore.Domain.Entities;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using FloraCore.Application.Interfaces;

namespace FloraCore.Tests.Application.Features.Orders.Queries;

/// <summary>
/// Contains unit tests for the GetMyOrdersQueryHandler class.
/// </summary>
public class GetMyOrdersQueryHandlerTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly GetMyOrdersQueryHandler _handler;

    /// <summary>
    /// Initializes a new instance of the GetMyOrdersQueryHandlerTests class.
    /// </summary>
    public GetMyOrdersQueryHandlerTests()
    {
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _handler = new GetMyOrdersQueryHandler(_mockOrderRepository.Object, _mockCurrentUserService.Object);
    }

    /// <summary>
    /// Verifies that Handle returns a list of orders for the currently authenticated user.
    /// </summary>
    [Fact]
    public async Task Handle_ValidQuery_ReturnsUserOrders()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);

        var orders = new List<Order>
        {
            new Order { Id = Guid.NewGuid(), UserId = userId, OrderStatus = "Pending", OrderDate = DateTime.UtcNow, TotalAmount = 100m },
            new Order { Id = Guid.NewGuid(), UserId = userId, OrderStatus = "Shipped", OrderDate = DateTime.UtcNow, TotalAmount = 250m }
        };
        _mockOrderRepository.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Order, bool>>>())).ReturnsAsync(2);
        _mockOrderRepository.Setup(r => r.GetWithOptionsAsync(It.IsAny<QueryOptions<Order>>())).ReturnsAsync(orders);

        var query = new GetMyOrdersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Items.All(r => r.UserId == userId).Should().BeTrue();
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(2);
    }

    /// <summary>
    /// Verifies that Handle returns filtered orders when an OrderStatus filter is supplied.
    /// </summary>
    [Fact]
    public async Task Handle_ValidQueryWithStatusFilter_AppliesFilterAndReturnsUserOrders()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);

        var orders = new List<Order>
        {
            new Order { Id = Guid.NewGuid(), UserId = userId, OrderStatus = "Pending", OrderDate = DateTime.UtcNow, TotalAmount = 100m }
        };
        _mockOrderRepository.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Order, bool>>>())).ReturnsAsync(1);
        _mockOrderRepository.Setup(r => r.GetWithOptionsAsync(It.IsAny<QueryOptions<Order>>())).ReturnsAsync(orders);

        var query = new GetMyOrdersQuery(OrderStatus: "Pending");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().OrderStatus.Should().Be("Pending");
    }

    /// <summary>
    /// Verifies that Handle throws UnauthorizedAccessException when the user is not authenticated.
    /// </summary>
    [Fact]
    public async Task Handle_UnauthenticatedUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _mockCurrentUserService.Setup(s => s.UserId).Returns((Guid?)null);

        var query = new GetMyOrdersQuery();

        // Act
        var act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("User not authenticated");
    }
}
