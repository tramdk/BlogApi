using FloraCore.Application.Common.Interfaces;
using FloraCore.Application.Features.Orders.Commands;
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

namespace FloraCore.Tests.Application.Features.Orders.Commands;

public class RemoveOrderItemCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly RemoveOrderItemCommandHandler _handler;

    public RemoveOrderItemCommandHandlerTests()
    {
        _mockOrderRepository = new Mock<IOrderRepository>();
        _handler = new RemoveOrderItemCommandHandler(_mockOrderRepository.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_RemovesOrderItem()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var orderItemId = Guid.NewGuid();
        var existingOrder = new Order { Id = orderId, TotalAmount = 20, OrderItems = new List<OrderItem> { new OrderItem { Id = orderItemId, Price = 10, Quantity = 2 } } };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(existingOrder);

        var command = new RemoveOrderItemCommand(orderId, orderItemId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockOrderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Once);
        _mockOrderRepository.Verify(r => r.DeleteOrderItemAsync(It.Is<OrderItem>(i => i.Id == orderItemId)), Times.Once);
        _mockOrderRepository.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.Id == orderId && o.TotalAmount == 0)), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidOrderId_ReturnsFalse()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync((Order)null);

        var command = new RemoveOrderItemCommand(orderId, Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockOrderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Once);
        _mockOrderRepository.Verify(r => r.DeleteOrderItemAsync(It.IsAny<OrderItem>()), Times.Never);
        _mockOrderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidOrderItemId_ReturnsFalse()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var orderItemId = Guid.NewGuid();
        var existingOrder = new Order { Id = orderId, TotalAmount = 20, OrderItems = new List<OrderItem>() };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(existingOrder);

        var command = new RemoveOrderItemCommand(orderId, orderItemId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockOrderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Once);
        _mockOrderRepository.Verify(r => r.DeleteOrderItemAsync(It.IsAny<OrderItem>()), Times.Never);
        _mockOrderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }
}
