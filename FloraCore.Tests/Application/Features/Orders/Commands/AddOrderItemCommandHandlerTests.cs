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

public class AddOrderItemCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<IGenericRepository<Product, Guid>> _mockProductRepository;
    private readonly AddOrderItemCommandHandler _handler;

    public AddOrderItemCommandHandlerTests()
    {
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockProductRepository = new Mock<IGenericRepository<Product, Guid>>();
        _handler = new AddOrderItemCommandHandler(_mockOrderRepository.Object, _mockProductRepository.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsOrderItem()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var existingOrder = new Order { Id = orderId, TotalAmount = 0, OrderItems = new List<OrderItem>() };
        var existingProduct = new Product { Id = productId, Price = 10 };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(existingOrder);
        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(existingProduct);

        var command = new AddOrderItemCommand(orderId, productId, 2, 10);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockOrderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Once);
        _mockProductRepository.Verify(r => r.GetByIdAsync(productId), Times.Once);
        _mockOrderRepository.Verify(r => r.AddOrderItemAsync(It.IsAny<OrderItem>()), Times.Once);
        _mockOrderRepository.Verify(r => r.UpdateAsync(It.Is<Order>(o => o.Id == orderId && o.TotalAmount == 20)), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidOrderId_ReturnsFalse()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync((Order)null);

        var command = new AddOrderItemCommand(orderId, Guid.NewGuid(), 2, 10);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockOrderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Once);
        _mockProductRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mockOrderRepository.Verify(r => r.AddOrderItemAsync(It.IsAny<OrderItem>()), Times.Never);
        _mockOrderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidProductId_ReturnsFalse()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var existingOrder = new Order { Id = orderId, TotalAmount = 0, OrderItems = new List<OrderItem>() };

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(existingOrder);
        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync((Product)null);

        var command = new AddOrderItemCommand(orderId, productId, 2, 10);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockOrderRepository.Verify(r => r.GetByIdAsync(orderId), Times.Once);
        _mockProductRepository.Verify(r => r.GetByIdAsync(productId), Times.Once);
        _mockOrderRepository.Verify(r => r.AddOrderItemAsync(It.IsAny<OrderItem>()), Times.Never);
        _mockOrderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }
}
