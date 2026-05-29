using FloraCore.Application.Features.WebsiteInfo.Commands;
using FloraCore.Application.Interfaces;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using FloraCore.Domain.Entities;

namespace FloraCore.Tests.Application.WebsiteInfo
{
    public class CreateWebsiteInfoCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ValidCommand_ReturnsWebsiteInfoId()
        {
            // Arrange
            var mockRepository = new Mock<IWebsiteInfoRepository>();
            var handler = new CreateWebsiteInfoCommandHandler(mockRepository.Object);
            var command = new CreateWebsiteInfoCommand(
                "Test Name",
                "Test Slogan",
                "Test Introduction",
                "test@example.com",
                "1234567890",
                "123456789",
                "Test Location"
            );

            Guid expectedId = Guid.NewGuid();
            mockRepository.Setup(repo => repo.AddAsync(It.IsAny<FloraCore.Domain.Entities.WebsiteInfo>()))
                .Returns(Task.CompletedTask)
                .Callback<FloraCore.Domain.Entities.WebsiteInfo>(websiteInfo => websiteInfo.Id = expectedId);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be(expectedId);
            mockRepository.Verify(repo => repo.AddAsync(It.IsAny<FloraCore.Domain.Entities.WebsiteInfo>()), Times.Once);
        }

        [Fact]
        public async Task Handle_InvalidCommand_ThrowsArgumentException()
        {
            // Arrange
            var mockRepository = new Mock<IWebsiteInfoRepository>();
            var handler = new CreateWebsiteInfoCommandHandler(mockRepository.Object);
            var command = new CreateWebsiteInfoCommand(
                "",
                "Test Slogan",
                "Test Introduction",
                "test@example.com",
                "1234567890",
                "123456789",
                "Test Location"
            );

            // Act
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Name cannot be null or empty. (Parameter 'Name')");
            mockRepository.Verify(repo => repo.AddAsync(It.IsAny<FloraCore.Domain.Entities.WebsiteInfo>()), Times.Never);
        }

        [Fact]
        public async Task Handle_InvalidEmail_ThrowsArgumentException()
        {
            // Arrange
            var mockRepository = new Mock<IWebsiteInfoRepository>();
            var handler = new CreateWebsiteInfoCommandHandler(mockRepository.Object);
            var command = new CreateWebsiteInfoCommand(
                "Test Name",
                "Test Slogan",
                "Test Introduction",
                "invalid-email",
                "1234567890",
                "123456789",
                "Test Location"
            );

            // Act
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Email is not valid. (Parameter 'Email')");
            mockRepository.Verify(repo => repo.AddAsync(It.IsAny<FloraCore.Domain.Entities.WebsiteInfo>()), Times.Never);
        }
    }
}
