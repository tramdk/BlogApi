using Xunit;
using FloraCore.Controllers;
using FloraCore.Application.Features.WebsiteInfo.Commands;
using FloraCore.Application.Features.WebsiteInfo.Queries;
using FloraCore.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;

namespace FloraCore.Tests.Web.Controllers
{
    public class WebsiteInfoControllerTests
    {
        [Fact]
        public async Task Get_ReturnsOkObjectResult_WithWebsiteInfo()
        {
            // Arrange
            var mediatorMock = new Mock<IMediator>();
            var controller = new WebsiteInfoController(mediatorMock.Object);
            var websiteInfoId = Guid.NewGuid();
            var expectedWebsiteInfo = new WebsiteInfo
            {
                Id = websiteInfoId,
                Name = "Test Name"
            };
            mediatorMock.Setup(m => m.Send(It.IsAny<GetWebsiteInfoQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedWebsiteInfo);

            // Act
            var result = await controller.Get(websiteInfoId);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().BeEquivalentTo(expectedWebsiteInfo);
        }

        [Fact]
        public async Task GetAll_ReturnsOkObjectResult_WithAllWebsiteInfos()
        {
            // Arrange
            var mediatorMock = new Mock<IMediator>();
            var controller = new WebsiteInfoController(mediatorMock.Object);
            var expectedWebsiteInfos = new List<WebsiteInfo>
            {
                new WebsiteInfo { Id = Guid.NewGuid(), Name = "Test Name 1" },
                new WebsiteInfo { Id = Guid.NewGuid(), Name = "Test Name 2" }
            };
            mediatorMock.Setup(m => m.Send(It.IsAny<GetAllWebsiteInfoQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedWebsiteInfos);

            // Act
            var result = await controller.GetAll();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().BeEquivalentTo(expectedWebsiteInfos);
        }

        [Fact]
        public async Task Create_ReturnsCreatedAtActionResult_WithWebsiteInfoId()
        {
            // Arrange
            var mediatorMock = new Mock<IMediator>();
            var controller = new WebsiteInfoController(mediatorMock.Object);
            var command = new CreateWebsiteInfoCommand(
                "Test Name",
                "Test Slogan",
                "Test Introduction",
                "test@example.com",
                "1234567890",
                "Tax-123456",
                "Test Location"
            );
            var expectedWebsiteInfoId = Guid.NewGuid();
            mediatorMock.Setup(m => m.Send(It.IsAny<CreateWebsiteInfoCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedWebsiteInfoId);

            // Act
            var result = await controller.Create(command);

            // Assert
            result.Should().BeOfType<CreatedAtActionResult>();
            var createdAtActionResult = result as CreatedAtActionResult;
            createdAtActionResult.ActionName.Should().Be(nameof(controller.Get));
            createdAtActionResult.Value.Should().Be(expectedWebsiteInfoId);
        }

        [Fact]
        public async Task Create_ReturnsBadRequest_WhenCommandIsInvalid()
        {
            // Arrange
            var mediatorMock = new Mock<IMediator>();
            var controller = new WebsiteInfoController(mediatorMock.Object);
            var command = new CreateWebsiteInfoCommand(
                null,
                null,
                null,
                null,
                null,
                null,
                null
            );
            mediatorMock.Setup(m => m.Send(It.IsAny<CreateWebsiteInfoCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new FluentValidation.ValidationException("Validation failed"));

            // Act
            var result = await controller.Create(command);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [Fact]
        public async Task Update_ReturnsNoContentResult_WhenUpdateIsSuccessful()
        {
            // Arrange
            var mediatorMock = new Mock<IMediator>();
            var controller = new WebsiteInfoController(mediatorMock.Object);
            var websiteInfoId = Guid.NewGuid();
            var command = new UpdateWebsiteInfoCommand(
                websiteInfoId,
                "Test Name",
                "Test Slogan",
                "Test Introduction",
                "test@example.com",
                "1234567890",
                "Tax-123456",
                "Test Location"
            );
            mediatorMock.Setup(m => m.Send(It.IsAny<UpdateWebsiteInfoCommand>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await controller.Update(websiteInfoId, command);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task Update_ReturnsBadRequest_WhenCommandIsInvalid()
        {
            // Arrange
            var mediatorMock = new Mock<IMediator>();
            var controller = new WebsiteInfoController(mediatorMock.Object);
            var websiteInfoId = Guid.NewGuid();
            var command = new UpdateWebsiteInfoCommand(
                websiteInfoId,
                null,
                null,
                null,
                null,
                null,
                null,
                null
            );
            mediatorMock.Setup(m => m.Send(It.IsAny<UpdateWebsiteInfoCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new FluentValidation.ValidationException("Validation failed"));

            // Act
            var result = await controller.Update(websiteInfoId, command);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [Fact]
        public async Task Delete_ReturnsNoContentResult_WhenDeleteIsSuccessful()
        {
            // Arrange
            var mediatorMock = new Mock<IMediator>();
            var controller = new WebsiteInfoController(mediatorMock.Object);
            var websiteInfoId = Guid.NewGuid();
            mediatorMock.Setup(m => m.Send(It.IsAny<DeleteWebsiteInfoCommand>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await controller.Delete(websiteInfoId);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }
    }
}
