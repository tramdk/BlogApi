using FloraCore.Application.Common.Models;
using FloraCore.Application.Features.Posts.DTOs;

namespace FloraCore.Application.Common.Interfaces;

public interface IPostQueryService
{
    Task<CursorPagedList<PostDto>> GetPostsAsync(Guid? cursor, int pageSize);
}
