using FloraCore.Application.Common.Interfaces;
using FloraCore.Application.Features.Products.Commands;
using FloraCore.Domain.Entities;
using FluentAssertions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FloraCore.Tests.Application.Features.Products.Commands;

public class CreateProductCommandHandlerTests
{
    private readonly Mock<IGenericRepository<Product, Guid>> _mockProductRepository;
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        _mockProductRepository = new Mock<IGenericRepository<Product, Guid>>();
        _handler = new CreateProductCommandHandler(_mockProductRepository.Object);
    }

    [Fact]
    public async Task Handle_Should_CreateProductWithPromotionRate_When_CommandIsValid()
    {
        // Arrange
        var command = new CreateProductCommand(
            Id: Guid.NewGuid(),
            Name: "Test Product",
            Description: "Test Description",
            Price: 100m,
            PromotionRate: 15.5m,
            Stock: 10,
            ImageUrl: "http://example.com/img.jpg",
            CategoryId: Guid.NewGuid()
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(command.Id!.Value);
        _mockProductRepository.Verify(x => x.AddAsync(It.Is<Product>(p =>
            p.Id == command.Id &&
            p.Name == command.Name &&
            p.Description == command.Description &&
            p.Price == command.Price &&
            p.PromotionRate == command.PromotionRate &&
            p.Stock == command.Stock &&
            p.ImageUrl == command.ImageUrl &&
            p.CategoryId == command.CategoryId)), Times.Once);
    }
}
