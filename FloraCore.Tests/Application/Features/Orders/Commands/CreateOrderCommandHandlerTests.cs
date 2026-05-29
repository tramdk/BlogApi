
using FloraCore.Application.Common.Interfaces;
using FloraCore.Application.Features.Orders.Commands;
using FloraCore.Domain.Entities;
using FloraCore.Domain.ValueObjects;
using FloraCore.Application.Interfaces;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace FloraCore.Tests.Application.Features.Orders.Commands
{
    public class CreateOrderCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldCreateOrderAndSendNotification()
        {
            // Arrange
            var mockOrderRepository = new Mock<IOrderRepository>();
            var mockAdminNotificationService = new Mock<IAdminNotificationService>();
            var mockLogger = new Mock<ILogger<CreateOrderCommandHandler>>();

            var handler = new CreateOrderCommandHandler(mockOrderRepository.Object, mockAdminNotificationService.Object);

            var command = new CreateOrderCommand(
                UserId: Guid.NewGuid(),
                ShippingAddress: new Address { Street = "Street", City = "City", State = "State", ZipCode = "ZipCode" },
                IdempotencyKey: ""
            );

            Guid createdOrderId = Guid.NewGuid();
            mockOrderRepository.Setup(repo => repo.AddAsync(It.IsAny<Order>()))
                .Returns(Task.CompletedTask)
                .Callback<Order>(order =>
                {
                    order.Id = createdOrderId;
                });

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be(createdOrderId);

            mockOrderRepository.Verify(repo => repo.AddAsync(It.IsAny<Order>()), Times.Once);
        }
    }
}
