using Xunit;
using FloraCore.Application.Features.WebsiteInfo.Commands;
using FloraCore.Application.Interfaces;
using FloraCore.Domain.Entities;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;

namespace FloraCore.Tests.Application.WebsiteInfoTests
{
    public class UpdateWebsiteInfoCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ValidCommand_UpdatesWebsiteInfo()
        {
            // Arrange
            var mockRepo = new Mock<IWebsiteInfoRepository>();
            var handler = new UpdateWebsiteInfoCommandHandler(mockRepo.Object);
            var command = new UpdateWebsiteInfoCommand(
                Guid.NewGuid(),
                "Test Name",
                "Test Slogan",
                "Test Introduction",
                "test@example.com",
                "1234567890",
                "Tax-123456",
                "Test Location"
            );
            mockRepo.Setup(repo => repo.GetByIdAsync(command.Id)).ReturnsAsync(new WebsiteInfo { Id = command.Id });
            mockRepo.Setup(repo => repo.UpdateAsync(It.IsAny<WebsiteInfo>())).Returns(Task.CompletedTask);

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            mockRepo.Verify(repo => repo.GetByIdAsync(command.Id), Times.Once);
            mockRepo.Verify(repo => repo.UpdateAsync(It.IsAny<WebsiteInfo>()), Times.Once);
        }

        [Fact]
        public async Task Handle_InvalidCommand_ThrowsException()
        {
            // Arrange
            var mockRepo = new Mock<IWebsiteInfoRepository>();
            var handler = new UpdateWebsiteInfoCommandHandler(mockRepo.Object);
            var command = new UpdateWebsiteInfoCommand(
                Guid.NewGuid(),
                "Test Name",
                "Test Slogan",
                "Test Introduction",
                "test@example.com",
                "1234567890",
                "Tax-123456",
                "Test Location"
            );
            mockRepo.Setup(repo => repo.GetByIdAsync(command.Id)).ReturnsAsync((WebsiteInfo)null);

            // Act
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
            mockRepo.Verify(repo => repo.GetByIdAsync(command.Id), Times.Once);
            mockRepo.Verify(repo => repo.UpdateAsync(It.IsAny<WebsiteInfo>()), Times.Never);
        }
    }
}
