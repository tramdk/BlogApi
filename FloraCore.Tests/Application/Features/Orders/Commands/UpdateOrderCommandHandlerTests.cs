using FloraCore.Application.Common.Interfaces;
using FloraCore.Application.Features.Orders.Commands;
using FloraCore.Domain.Entities;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using FloraCore.Application.Interfaces;

namespace FloraCore.Tests.Application.Features.Orders.Commands;

public class UpdateOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly UpdateOrderCommandHandler _handler;

    public UpdateOrderCommandHandlerTests()
    {
        _mockOrderRepository = new Mock<IOrderRepository>();
        _handler = new UpdateOrderCommandHandler(_mockOrderRepository.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = new Order { Id = orderId, OrderStatus = "Pending" };
        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(existingOrder);

        var command = new UpdateOrderCommand(orderId, "Shipped");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockOrderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Once);
        _mockOrderRepository.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.Id == orderId && o.OrderStatus == "Shipped")), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidOrderId_ReturnsFalse()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync((Order)null);

        var command = new UpdateOrderCommand(orderId, "Shipped");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockOrderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Once);
        _mockOrderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }
}
