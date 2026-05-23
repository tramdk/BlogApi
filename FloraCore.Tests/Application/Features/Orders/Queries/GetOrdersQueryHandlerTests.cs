using FloraCore.Application.Common.Interfaces;
using FloraCore.Application.Features.Orders.Queries;
using FloraCore.Domain.Entities;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using FloraCore.Application.Interfaces;

namespace FloraCore.Tests.Application.Features.Orders.Queries;

public class GetOrdersQueryHandlerTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly GetOrdersQueryHandler _handler;

    public GetOrdersQueryHandlerTests()
    {
        _mockOrderRepository = new Mock<IOrderRepository>();
        _handler = new GetOrdersQueryHandler(_mockOrderRepository.Object);
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsListOfOrderDtos()
    {
        // Arrange
        var orders = new List<Order>
        {
            new Order { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), OrderStatus = "Pending" },
            new Order { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), OrderStatus = "Shipped" }
        };
        _mockOrderRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(orders);

        var query = new GetOrdersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(r => r is OrderDto).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NoOrders_ReturnsEmptyList()
    {
        // Arrange
        _mockOrderRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Order>());

        var query = new GetOrdersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}
