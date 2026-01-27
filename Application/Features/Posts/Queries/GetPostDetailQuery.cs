using BlogApi.Application.Common.Attributes;
using BlogApi.Application.Common.Interfaces;
using BlogApi.Domain.Entities;
using BlogApi.Application.Features.Posts.DTOs;
using BlogApi.Application.Common.Models;
using MediatR;

namespace BlogApi.Application.Features.Posts.Queries;

[Cacheable(ExpirationMinutes = 5)]
public record GetPostDetailQuery(Guid Id) : IRequest<PostDetailDto>;

public class GetPostDetailHandler : IRequestHandler<GetPostDetailQuery, PostDetailDto>
{
    private readonly IGenericRepository<Post, Guid> _postRepository;
    public GetPostDetailHandler(IGenericRepository<Post, Guid> postRepository) => _postRepository = postRepository;

    public async Task<PostDetailDto> Handle(GetPostDetailQuery request, CancellationToken cancellationToken)
    {
        var options = new QueryOptionsBuilder<Post>()
            .WithFilter(p => p.Id == request.Id)
            .WithInclude(p => p.Author)
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
