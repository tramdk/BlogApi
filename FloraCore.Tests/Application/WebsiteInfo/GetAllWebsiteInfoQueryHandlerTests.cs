using Xunit;
using FloraCore.Application.Features.WebsiteInfo.Queries;
using FloraCore.Application.Interfaces;
using FloraCore.Domain.Entities;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;

namespace FloraCore.Tests.Application.WebsiteInfoTests
{
    public class GetAllWebsiteInfoQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ValidQuery_ReturnsAllWebsiteInfo()
        {
            // Arrange
            var mockRepo = new Mock<IWebsiteInfoRepository>();
            var handler = new GetAllWebsiteInfoQueryHandler(mockRepo.Object);
            var expectedWebsiteInfos = new List<WebsiteInfo>
            {
                new WebsiteInfo { Id = Guid.NewGuid(), Name = "Test Name 1" },
                new WebsiteInfo { Id = Guid.NewGuid(), Name = "Test Name 2" }
            };
            mockRepo.Setup(repo => repo.GetAllAsync()).ReturnsAsync(expectedWebsiteInfos);

            // Act
            var result = await handler.Handle(new GetAllWebsiteInfoQuery(), CancellationToken.None);

            // Assert
            result.Should().BeEquivalentTo(expectedWebsiteInfos);
            mockRepo.Verify(repo => repo.GetAllAsync(), Times.Once);
        }
    }
}
