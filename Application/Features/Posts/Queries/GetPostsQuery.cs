using FloraCore.Application.Common.Attributes;
using FloraCore.Application.Common.Interfaces;
using FloraCore.Application.Common.Models;
using FloraCore.Application.Features.Posts.DTOs;
using MediatR;

namespace FloraCore.Application.Features.Posts.Queries;

[Cacheable(ExpirationMinutes = 5)]
public record GetPostsQuery(Guid? Cursor = null, int PageSize = 10) : IRequest<CursorPagedList<PostDto>>;

public class GetPostsHandler(IPostQueryService postQueryService) : IRequestHandler<GetPostsQuery, CursorPagedList<PostDto>>
{
    private readonly IPostQueryService _postQueryService = postQueryService ?? throw new ArgumentNullException(nameof(postQueryService));

    public async Task<CursorPagedList<PostDto>> Handle(GetPostsQuery request, CancellationToken cancellationToken)
    {
        return await _postQueryService.GetPostsAsync(request.Cursor, request.PageSize);
    }
}
