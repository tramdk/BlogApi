using BlogApi.Application.Common.Interfaces;
using BlogApi.Domain.Entities;
using MediatR;
using UUIDNext;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlogApi.Application.Features.Posts.Commands;

public record CreatePostCommand(string Title, string Content, string? CategoryId = null) : IRequest<Guid>;

public class CreatePostHandler : IRequestHandler<CreatePostCommand, Guid>
{
    private readonly IGenericRepository<Post, Guid> _postRepository;
    private readonly ICurrentUserService _currentUserService;

    public CreatePostHandler(IGenericRepository<Post, Guid> postRepository, ICurrentUserService currentUserService)
    {
        _postRepository = postRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();

        var post = new Post 
        { 
            Id = Uuid.NewDatabaseFriendly(Database.SqlServer), 
            Title = request.Title, 
            Content = request.Content, 
            AuthorId = userId,
            CategoryId = request.CategoryId
        };
        
        await _postRepository.AddAsync(post);
        return post.Id;
    }
}
