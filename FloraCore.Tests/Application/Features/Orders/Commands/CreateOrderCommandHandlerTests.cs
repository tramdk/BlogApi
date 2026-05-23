using FloraCore.Application.Common.Interfaces;
using FloraCore.Application.Features.Orders.Commands;
using FloraCore.Domain.Entities;
using FloraCore.Domain.ValueObjects;
using MediatR;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using FloraCore.Application.Interfaces;

namespace FloraCore.Tests.Application.Features.Orders.Commands;

public class CreateOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        _mockOrderRepository = new Mock<IOrderRepository>();
        _handler = new CreateOrderCommandHandler(_mockOrderRepository.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesOrder()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var command = new CreateOrderCommand(userId, new Address { Street = "Test Street", City = "Test City", State = "Test State", ZipCode = "12345", Country = "Test Country" });

        Guid createdOrderId = Guid.Empty;
        _mockOrderRepository.Setup(r => r.AddAsync(It.IsAny<Order>()))
            .Callback<Order>(order => createdOrderId = order.Id)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _mockOrderRepository.Verify(r => r.AddAsync(It.Is<Order>(o => o.UserId == userId)), Times.Once);
    }
}
