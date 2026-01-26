using BlogApi.Application.Common.Models;
using BlogApi.Application.Features.Posts.DTOs;

namespace BlogApi.Application.Common.Interfaces;

public interface IPostQueryService
{
    Task<CursorPagedList<PostDto>> GetPostsAsync(Guid? cursor, int pageSize);
}
