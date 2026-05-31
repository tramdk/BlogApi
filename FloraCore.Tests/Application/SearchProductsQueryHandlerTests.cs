using FloraCore.Application.Interfaces;
using FloraCore.Application.Features.Products.Queries;
using FloraCore.Application.Features.Products.DTOs;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace FloraCore.Tests.Application;

public class SearchProductsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnSearchResults_WhenSearchTermIsProvided()
    {
        // Arrange
        var productRepositoryMock = new Mock<IProductRepository>();
        var searchTerm = "test";
        var expectedResults = new List<ProductSearchResultDto>
        {
            new ProductSearchResultDto 
            { 
                Id = Guid.NewGuid(), 
                Name = "Test Product 1", 
                Description = "Description 1",
                Price = 99.99m,
                Stock = 50,
                ImageUrl = "http://example.com/image1.jpg",
                AverageRating = 4.5
            },
            new ProductSearchResultDto 
            { 
                Id = Guid.NewGuid(), 
                Name = "Test Product 2", 
                Description = "Description 2",
                Price = 149.99m,
                Stock = 25,
                ImageUrl = "http://example.com/image2.jpg",
                AverageRating = 4.8
            }
        };

        productRepositoryMock.Setup(x => x.SearchProductsAsync(searchTerm)).ReturnsAsync(expectedResults);

        var handler = new SearchProductsQueryHandler(productRepositoryMock.Object);
        var query = new SearchProductsQuery(searchTerm);

        // Act
        var actualResults = await handler.Handle(query, CancellationToken.None);

        // Assert
        actualResults.Should().BeEquivalentTo(expectedResults);
        productRepositoryMock.Verify(x => x.SearchProductsAsync(searchTerm), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenSearchTermIsNotFound()
    {
        // Arrange
        var productRepositoryMock = new Mock<IProductRepository>();
        var searchTerm = "nonexistent";
        var expectedResults = new List<ProductSearchResultDto>();

        productRepositoryMock.Setup(x => x.SearchProductsAsync(searchTerm)).ReturnsAsync(expectedResults);

        var handler = new SearchProductsQueryHandler(productRepositoryMock.Object);
        var query = new SearchProductsQuery(searchTerm);

        // Act
        var actualResults = await handler.Handle(query, CancellationToken.None);

        // Assert
        actualResults.Should().BeEquivalentTo(expectedResults);
        productRepositoryMock.Verify(x => x.SearchProductsAsync(searchTerm), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowArgumentNullException_WhenProductRepositoryIsNull()
    {
        // Arrange
        IProductRepository productRepository = null!;

        // Act
        var act = () => new SearchProductsQueryHandler(productRepository);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
