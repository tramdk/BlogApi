using AutoFixture;
using FluentAssertions;
using FloraCore.Application.Common.Interfaces;
using FloraCore.Application.Features.ProductCategories.Commands;
using FloraCore.Domain.Entities;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FloraCore.Tests.Application.Features.ProductCategories.Commands;

public class CreateProductCategoryCommandTests
{
    private readonly Mock<IGenericRepository<ProductCategory, Guid>> _mockRepository;
    private readonly IFixture _fixture;

    public CreateProductCategoryCommandTests()
    {
        _mockRepository = new Mock<IGenericRepository<ProductCategory, Guid>>();
        _fixture = new Fixture();
    }

    [Fact]
    public async Task Handle_ShouldCreateProductCategory_WhenCommandIsValid()
    {
        // Arrange
        var command = _fixture.Build<CreateProductCategoryCommand>()
                              .With(c => c.Id, (Guid?)null) // Ensure Id is null for auto-generation
                              .Create();
        var handler = new CreateProductCategoryCommandHandler(_mockRepository.Object);

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ProductCategory>()))
                       .Returns(Task.CompletedTask);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _mockRepository.Verify(r => r.AddAsync(It.Is<ProductCategory>(pc =>
            pc.Name == command.Name &&
            pc.Description == command.Description &&
            pc.ImageUrl == command.ImageUrl &&
            pc.Id == result
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCreateProductCategoryWithProvidedId_WhenIdIsNotNull()
    {
        // Arrange
        var providedId = Guid.NewGuid();
        var command = _fixture.Build<CreateProductCategoryCommand>()
                              .With(c => c.Id, providedId)
                              .Create();
        var handler = new CreateProductCategoryCommandHandler(_mockRepository.Object);

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ProductCategory>()))
                       .Returns(Task.CompletedTask);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(providedId);
        _mockRepository.Verify(r => r.AddAsync(It.Is<ProductCategory>(pc =>
            pc.Name == command.Name &&
            pc.Description == command.Description &&
            pc.ImageUrl == command.ImageUrl &&
            pc.Id == providedId
        )), Times.Once);
    }
}
