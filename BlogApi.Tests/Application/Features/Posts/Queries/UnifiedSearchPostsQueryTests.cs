using BlogApi.Application.Common.Interfaces;
using BlogApi.Application.Common.Models;
using BlogApi.Application.Features.Posts.DTOs;
using BlogApi.Application.Features.Posts.Queries;
using BlogApi.Domain.Entities;
using Moq;
using Xunit;
using FluentAssertions;

namespace BlogApi.Tests.Application.Features.Posts.Queries;

public class UnifiedSearchPostsQueryTests
{
    private readonly Mock<IGenericRepository<Post, Guid>> _mockRepo;
    private readonly UnifiedSearchPostsHandler _handler;

    public UnifiedSearchPostsQueryTests()
    {
        _mockRepo = new Mock<IGenericRepository<Post, Guid>>();
        _handler = new UnifiedSearchPostsHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ShouldCallRepository_WithSimpleSearchRequest()
    {
        // Arrange
        var request = new UnifiedSearchRequest
        {
            SearchTerm = "test",
            Page = 1,
            PageSize = 10
        };
        var query = new UnifiedSearchPostsQuery(request);
        
        var pagedResult = new PagedResult<Post>(new List<Post>(), 0, 0, 10);
        _mockRepo.Setup(x => x.GetPagedAsync(It.IsAny<QueryOptions<Post>>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockRepo.Verify(x => x.GetPagedAsync(It.Is<QueryOptions<Post>>(o => 
            o.Take == 10 && o.Skip == 0)), Times.Once);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ShouldCallRepository_WithFilterModelRequest()
    {
        // Arrange
        var request = new UnifiedSearchRequest
        {
            Filters = new Dictionary<string, FilterCondition>
            {
                { "title", new FilterCondition { FilterType = "text", Type = "contains", Filter = "react" } }
            },
            Page = 0,
            PageSize = 10
        };
        var query = new UnifiedSearchPostsQuery(request);
        
        var pagedResult = new PagedResult<Post>(new List<Post>(), 0, 0, 10);
        _mockRepo.Setup(x => x.GetPagedAsync(It.IsAny<QueryOptions<Post>>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockRepo.Verify(x => x.GetPagedAsync(It.IsAny<QueryOptions<Post>>()), Times.Once);
        result.Should().NotBeNull();
    }
}
