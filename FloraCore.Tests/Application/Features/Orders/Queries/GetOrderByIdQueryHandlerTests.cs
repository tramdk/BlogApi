using FloraCore.Application.Common.Interfaces;
using FloraCore.Application.Features.Orders.Queries;
using FloraCore.Domain.Entities;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using FloraCore.Application.Interfaces;
using FloraCore.Application.Common.Models;

namespace FloraCore.Tests.Application.Features.Orders.Queries;

public class GetOrderByIdQueryHandlerTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly GetOrderByIdQueryHandler _handler;

    public GetOrderByIdQueryHandlerTests()
    {
        _mockOrderRepository = new Mock<IOrderRepository>();
        _handler = new GetOrderByIdQueryHandler(_mockOrderRepository.Object);
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsOrderDto()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = new Order { Id = orderId, UserId = Guid.NewGuid(), OrderStatus = "Pending" };
        _mockOrderRepository.Setup(r => r.GetSingleWithOptionsAsync(It.IsAny<QueryOptions<Order>>()))
            .ReturnsAsync(existingOrder);

        var query = new GetOrderByIdQuery(orderId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(orderId);
        result.UserId.Should().Be(existingOrder.UserId);
        result.OrderStatus.Should().Be(existingOrder.OrderStatus);
    }

    [Fact]
    public async Task Handle_InvalidOrderId_ReturnsNull()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _mockOrderRepository.Setup(r => r.GetSingleWithOptionsAsync(It.IsAny<QueryOptions<Order>>()))
            .ReturnsAsync((Order?)null);

        var query = new GetOrderByIdQuery(orderId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _mockOrderRepository.Verify(r => r.GetSingleWithOptionsAsync(It.IsAny<QueryOptions<Order>>()), Times.Once);
    }
}
