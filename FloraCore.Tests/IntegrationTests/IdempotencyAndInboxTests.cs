using FloraCore.Application.Common.Interfaces;
using FloraCore.Application.Features.Orders.Commands;
using FloraCore.Domain.ValueObjects;
using FloraCore.Infrastructure.Data;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace FloraCore.Tests.IntegrationTests;

/// <summary>
/// Integration tests verifying the idempotency mechanism and inbox pattern implementations.
/// </summary>
public class IdempotencyAndInboxTests(CustomWebApplicationFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_DuplicateIdempotentCommand_ReturnsCachedResponseAndDoesNotCreateDuplicate()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await dbContext.Users.FirstAsync();
        var userId = user.Id;
        var address = new Address
        {
            Street = "Idempotent St",
            City = "Idempotent City",
            State = "Idempotent State",
            ZipCode = "99999",
            Country = "Idempotent Country"
        };
        var idempotencyKey = Guid.NewGuid().ToString();
        var command = new CreateOrderCommand(userId, address, idempotencyKey);

        // Act - Call 1
        var orderId1 = await mediator.Send(command);

        // Act - Call 2 (Duplicate request with same idempotency key)
        var orderId2 = await mediator.Send(command);

        // Assert
        orderId1.Should().NotBeEmpty();
        orderId2.Should().Be(orderId1); // Must return the exact same order ID

        // Verify only 1 order exists in database for this key / user
        var ordersInDb = await dbContext.Orders
            .Where(o => o.UserId == userId)
            .ToListAsync();
        ordersInDb.Should().HaveCount(1);
    }

    [Fact]
    public async Task InboxService_ShouldPreventDuplicateProcessing()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var inboxService = scope.ServiceProvider.GetRequiredService<IInboxService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var messageId = Guid.NewGuid();
        var eventName = "OrderCreatedEvent";

        // Act & Assert 1: New message should not be processed yet
        var alreadyProcessed = await inboxService.HasBeenProcessedAsync(messageId);
        alreadyProcessed.Should().BeFalse();

        // Act 2: Mark message as processed
        await inboxService.MarkAsProcessedAsync(messageId, eventName);

        // Assert 2: Message should be marked as processed
        var processedNow = await inboxService.HasBeenProcessedAsync(messageId);
        processedNow.Should().BeTrue();

        // Verify record in DbContext
        var inboxMsg = await dbContext.InboxMessages.FirstOrDefaultAsync(m => m.Id == messageId);
        inboxMsg.Should().NotBeNull();
        inboxMsg!.EventName.Should().Be(eventName);
    }
}
