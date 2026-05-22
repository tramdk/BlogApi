using Xunit;
using FloraCore.Application.Features.WebsiteInfo.Queries;
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
    public class GetWebsiteInfoQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ValidQuery_ReturnsWebsiteInfo()
        {
            // Arrange
            var mockRepo = new Mock<IWebsiteInfoRepository>();
            var handler = new GetWebsiteInfoQueryHandler(mockRepo.Object);
            var websiteInfoId = Guid.NewGuid();
            var expectedWebsiteInfo = new WebsiteInfo { Id = websiteInfoId, Name = "Test Name" };
            mockRepo.Setup(repo => repo.GetByIdAsync(websiteInfoId)).ReturnsAsync(expectedWebsiteInfo);

            // Act
            var result = await handler.Handle(new GetWebsiteInfoQuery(websiteInfoId), CancellationToken.None);

            // Assert
            result.Should().BeEquivalentTo(expectedWebsiteInfo);
            mockRepo.Verify(repo => repo.GetByIdAsync(websiteInfoId), Times.Once);
        }

        [Fact]
        public async Task Handle_InvalidQuery_ReturnsNull()
        {
            // Arrange
            var mockRepo = new Mock<IWebsiteInfoRepository>();
            var handler = new GetWebsiteInfoQueryHandler(mockRepo.Object);
            var websiteInfoId = Guid.NewGuid();
            mockRepo.Setup(repo => repo.GetByIdAsync(websiteInfoId)).ReturnsAsync((WebsiteInfo)null);

            // Act
            var result = await handler.Handle(new GetWebsiteInfoQuery(websiteInfoId), CancellationToken.None);

            // Assert
            result.Should().BeNull();
            mockRepo.Verify(repo => repo.GetByIdAsync(websiteInfoId), Times.Once);
        }
    }
}
