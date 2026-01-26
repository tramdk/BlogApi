using BlogApi.Application.Common.Interfaces;
using BlogApi.Application.Features.Posts.Commands;
using BlogApi.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

namespace BlogApi.Tests.Application.Features.Posts.Commands;

public class CreatePostHandlerTests
{
    private readonly Mock<IGenericRepository<Post, Guid>> _mockPostRepository;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly CreatePostHandler _handler;

    public CreatePostHandlerTests()
    {
        _mockPostRepository = new Mock<IGenericRepository<Post, Guid>>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _handler = new CreatePostHandler(_mockPostRepository.Object, _mockCurrentUserService.Object);
    }

    [Fact]
    public async Task Handle_Should_CreatePost_When_UserIsAuthenticated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);

        var command = new CreatePostCommand("Test Title", "Test Content");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _mockPostRepository.Verify(x => x.AddAsync(It.Is<Post>(p => 
            p.Title == command.Title && 
            p.Content == command.Content && 
            p.AuthorId == userId)), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ThrowUnauthorizedAccessException_When_UserIsNotAuthenticated()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.UserId).Returns((Guid?)null);

        var command = new CreatePostCommand("Test Title", "Test Content");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _mockPostRepository.Verify(x => x.AddAsync(It.IsAny<Post>()), Times.Never);
    }
}
