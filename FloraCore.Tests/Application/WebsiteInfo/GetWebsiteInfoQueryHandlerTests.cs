using Xunit;
using FloraCore.Application.Features.WebsiteInfo.Queries;
using FloraCore.Application.Interfaces;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FloraCore.Domain.Entities;

namespace FloraCore.Tests.Application.WebsiteInfo
{
    public class GetWebsiteInfoQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ExistingId_ReturnsWebsiteInfo()
        {
            // Arrange
            var mockRepository = new Mock<IWebsiteInfoRepository>();
            var handler = new GetWebsiteInfoQueryHandler(mockRepository.Object);
            var expectedWebsiteInfo = new FloraCore.Domain.Entities.WebsiteInfo { Id = Guid.NewGuid(), Name = "Test Name" };
            mockRepository.Setup(repo => repo.GetByIdAsync(expectedWebsiteInfo.Id)).ReturnsAsync(expectedWebsiteInfo);

            // Act
            var result = await handler.Handle(new GetWebsiteInfoQuery(expectedWebsiteInfo.Id), CancellationToken.None);

            // Assert
            result.Should().BeEquivalentTo(expectedWebsiteInfo);
            mockRepository.Verify(repo => repo.GetByIdAsync(expectedWebsiteInfo.Id), Times.Once);
        }

        [Fact]
        public async Task Handle_NonExistingId_ReturnsNull()
        {
            // Arrange
            var mockRepository = new Mock<IWebsiteInfoRepository>();
            var handler = new GetWebsiteInfoQueryHandler(mockRepository.Object);
            Guid nonExistingId = Guid.NewGuid();
            mockRepository.Setup(repo => repo.GetByIdAsync(nonExistingId)).ReturnsAsync((FloraCore.Domain.Entities.WebsiteInfo)null);

            // Act
            var result = await handler.Handle(new GetWebsiteInfoQuery(nonExistingId), CancellationToken.None);

            // Assert
            result.Should().BeNull();
            mockRepository.Verify(repo => repo.GetByIdAsync(nonExistingId), Times.Once);
        }
    }
}
