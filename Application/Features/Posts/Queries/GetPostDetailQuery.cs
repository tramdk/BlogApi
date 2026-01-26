using BlogApi.Application.Common.Attributes;
using BlogApi.Application.Common.Interfaces;
using BlogApi.Domain.Entities;
using BlogApi.Application.Features.Posts.DTOs;
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
        var post = await _postRepository.GetByIdAsync(request.Id);
        if (post == null) return null!;

        return new PostDetailDto(post.Id, post.Title, post.Content, post.AverageRating, post.TotalRatings, post.CategoryId);
    }
}
