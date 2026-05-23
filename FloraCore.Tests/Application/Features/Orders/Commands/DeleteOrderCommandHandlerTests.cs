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

public class DeleteOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly DeleteOrderCommandHandler _handler;

    public DeleteOrderCommandHandlerTests()
    {
        _mockOrderRepository = new Mock<IOrderRepository>();
        _handler = new DeleteOrderCommandHandler(_mockOrderRepository.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_DeletesOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = new Order { Id = orderId };
        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(existingOrder);

        var command = new DeleteOrderCommand(orderId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockOrderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Once);
        _mockOrderRepository.Verify(r => r.DeleteAsync(existingOrder), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidOrderId_ReturnsFalse()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync((Order)null);

        var command = new DeleteOrderCommand(orderId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockOrderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Once);
        _mockOrderRepository.Verify(r => r.DeleteAsync(It.IsAny<Order>()), Times.Never);
    }
}
