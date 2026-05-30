
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
using MediatR;
using FloraCore.Application.Features.Orders.Events;

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
            var mockMediator = new Mock<IMediator>();
            var mockLogger = new Mock<ILogger<CreateOrderCommandHandler>>();

            var handler = new CreateOrderCommandHandler(
                mockOrderRepository.Object, 
                mockAdminNotificationService.Object,
                mockMediator.Object);

            var command = new CreateOrderCommand(
                UserId: Guid.NewGuid(),
                ShippingAddress: new Address { Street = "Street", City = "City", State = "State", ZipCode = "ZipCode" },
                IdempotencyKey: ""
            );

            Order? savedOrder = null;
            Guid createdOrderId = Guid.NewGuid();
            mockOrderRepository.Setup(repo => repo.AddAsync(It.IsAny<Order>()))
                .Returns(Task.CompletedTask)
                .Callback<Order>(order =>
                {
                    order.Id = createdOrderId;
                    savedOrder = order;
                });

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be(createdOrderId);

            mockOrderRepository.Verify(repo => repo.AddAsync(It.IsAny<Order>()), Times.Once);
            mockMediator.Verify(m => m.Publish(It.Is<OrderCreatedEvent>(e => e.OrderId == createdOrderId), It.IsAny<CancellationToken>()), Times.Once);
            savedOrder.Should().NotBeNull();
            savedOrder!.StatusHistories.Should().HaveCount(1);
            var history = savedOrder.StatusHistories.GetEnumerator();
            history.MoveNext().Should().BeTrue();
            history.Current.FromStatus.Should().BeEmpty();
            history.Current.ToStatus.Should().Be("Pending");
        }
    }
}
