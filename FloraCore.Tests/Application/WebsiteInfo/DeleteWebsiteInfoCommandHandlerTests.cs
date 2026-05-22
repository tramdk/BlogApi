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
    public class DeleteWebsiteInfoCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ValidCommand_DeletesWebsiteInfo()
        {
            // Arrange
            var mockRepo = new Mock<IWebsiteInfoRepository>();
            var handler = new DeleteWebsiteInfoCommandHandler(mockRepo.Object);
            var websiteInfoId = Guid.NewGuid();
            mockRepo.Setup(repo => repo.GetByIdAsync(websiteInfoId)).ReturnsAsync(new WebsiteInfo { Id = websiteInfoId });
            mockRepo.Setup(repo => repo.DeleteAsync(websiteInfoId)).Returns(Task.CompletedTask);

            // Act
            await handler.Handle(new DeleteWebsiteInfoCommand(websiteInfoId), CancellationToken.None);

            // Assert
            mockRepo.Verify(repo => repo.GetByIdAsync(websiteInfoId), Times.Once);
            mockRepo.Verify(repo => repo.DeleteAsync(websiteInfoId), Times.Once);
        }

        [Fact]
        public async Task Handle_InvalidCommand_ThrowsException()
        {
            // Arrange
            var mockRepo = new Mock<IWebsiteInfoRepository>();
            var handler = new DeleteWebsiteInfoCommandHandler(mockRepo.Object);
            var websiteInfoId = Guid.NewGuid();
            mockRepo.Setup(repo => repo.GetByIdAsync(websiteInfoId)).ReturnsAsync((WebsiteInfo)null);

            // Act
            Func<Task> act = async () => await handler.Handle(new DeleteWebsiteInfoCommand(websiteInfoId), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
            mockRepo.Verify(repo => repo.GetByIdAsync(websiteInfoId), Times.Once);
            mockRepo.Verify(repo => repo.DeleteAsync(websiteInfoId), Times.Never);
        }
    }
}
