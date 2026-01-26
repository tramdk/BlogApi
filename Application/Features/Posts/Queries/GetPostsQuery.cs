using BlogApi.Application.Common.Attributes;
using BlogApi.Application.Common.Interfaces;
using BlogApi.Application.Common.Models;
using BlogApi.Application.Features.Posts.DTOs;
using MediatR;

namespace BlogApi.Application.Features.Posts.Queries;

[Cacheable(ExpirationMinutes = 5)]
public record GetPostsQuery(Guid? Cursor = null, int PageSize = 10) : IRequest<CursorPagedList<PostDto>>;

public class GetPostsHandler : IRequestHandler<GetPostsQuery, CursorPagedList<PostDto>>
{
    private readonly IPostQueryService _postQueryService;

    public GetPostsHandler(IPostQueryService postQueryService)
    {
        _postQueryService = postQueryService;
    }

    public async Task<CursorPagedList<PostDto>> Handle(GetPostsQuery request, CancellationToken cancellationToken)
    {
        return await _postQueryService.GetPostsAsync(request.Cursor, request.PageSize);
    }
}
