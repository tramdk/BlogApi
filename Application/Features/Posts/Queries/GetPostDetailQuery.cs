using FloraCore.Application.Common.Attributes;
using FloraCore.Application.Common.Interfaces;
using FloraCore.Domain.Entities;
using FloraCore.Application.Features.Posts.DTOs;
using FloraCore.Application.Common.Models;
using MediatR;

namespace FloraCore.Application.Features.Posts.Queries;

[Cacheable(ExpirationMinutes = 5)]
public record GetPostDetailQuery(Guid Id) : IRequest<PostDetailDto>;

/// <summary>
/// Handler for getting post details by ID.
/// </summary>
/// <param name="postRepository">The repository for accessing post data.</param>
public class GetPostDetailHandler(IGenericRepository<Post, Guid> postRepository) : IRequestHandler<GetPostDetailQuery, PostDetailDto>
{
    private readonly IGenericRepository<Post, Guid> _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));

    /// <summary>
    /// Handles the request to get post details.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A PostDetailDto if found; otherwise, null.</returns>
    public async Task<PostDetailDto> Handle(GetPostDetailQuery request, CancellationToken cancellationToken)
    {
        var options = new QueryOptionsBuilder<Post>()
            .WithFilter(p => p.Id == request.Id)
            .WithInclude(p => p.Author)
            .AsNoTracking()
            .Build();

        var post = await _postRepository.GetSingleWithOptionsAsync(options);
        
        if (post == null) return null!;

        return new PostDetailDto(
            post.Id, 
            post.Title, 
            post.Content, 
            post.AverageRating, 
            post.TotalRatings, 
            post.CategoryId,
            post.Author?.FullName,
            post.CreatedAt,
            post.UpdatedAt
        );
    }
}
