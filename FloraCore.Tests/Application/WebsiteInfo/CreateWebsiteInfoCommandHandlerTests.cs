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
    public class CreateWebsiteInfoCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ValidCommand_CreatesWebsiteInfo()
        {
            // Arrange
            var mockRepo = new Mock<IWebsiteInfoRepository>();
            var handler = new CreateWebsiteInfoCommandHandler(mockRepo.Object);
            var command = new CreateWebsiteInfoCommand(
                "Test Name",
                "Test Slogan",
                "Test Introduction",
                "test@example.com",
                "1234567890",
                "Tax-123456",
                "Test Location"
            );
            Guid expectedId = Guid.NewGuid();
            mockRepo.Setup(repo => repo.AddAsync(It.IsAny<WebsiteInfo>())).ReturnsAsync(expectedId);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be(expectedId);
            mockRepo.Verify(repo => repo.AddAsync(It.IsAny<WebsiteInfo>()), Times.Once);
        }

        [Fact]
        public async Task Handle_InvalidCommand_ThrowsException()
        {
            // Arrange
            var mockRepo = new Mock<IWebsiteInfoRepository>();
            var handler = new CreateWebsiteInfoCommandHandler(mockRepo.Object);
            var command = new CreateWebsiteInfoCommand(
                null,
                "Test Slogan",
                "Test Introduction",
                "test@example.com",
                "1234567890",
                "Tax-123456",
                "Test Location"
            );

            // Act
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
            mockRepo.Verify(repo => repo.AddAsync(It.IsAny<WebsiteInfo>()), Times.Never);
        }
    }
}
