using FloraCore.Application.Common.Interfaces;
using FloraCore.Application.Common.Models;
using FloraCore.Application.Features.Posts.DTOs;
using FloraCore.Infrastructure.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FloraCore.Infrastructure.Repositories;

/// <summary>
/// Implementation of IPostQueryService using Dapper for optimal read performance with cursor pagination.
/// Utilizes the Strategy Pattern (IPostQueryDialect) to support multiple database dialects safely.
/// </summary>
public class PostQueryService(AppDbContext context, IPostQueryDialect dialect, ILogger<PostQueryService> logger) : IPostQueryService
{
    private const string NextCursor = "NextCursor";
    private readonly AppDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly IPostQueryDialect _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
    private readonly ILogger<PostQueryService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private const int PageSizeIncrement = 1;

    /// <summary>
    /// Retrieves a cursor-paged list of posts.
    /// </summary>
    /// <param name="cursor">The cursor for pagination.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A cursor-paged list of posts.</returns>
    public async Task<CursorPagedList<PostDto>> GetPostsAsync(Guid? cursor, int pageSize)
    {
        using var connection = _context.Database.GetDbConnection();

        // Retrieve the dialect-specific SQL string
        var sql = _dialect.GetPostsSql();

        var parameters = new { Cursor = cursor, PageSizePlusOne = pageSize + PageSizeIncrement };

        List<PostDto> posts;
        try
        {
            posts = (await connection.QueryAsync<PostDto>(sql, parameters)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying posts with Dapper.");
            throw;
        }

        var hasNextPage = posts.Count > pageSize;
        if (hasNextPage)
        {
            posts.RemoveAt(pageSize);
        }

        var nextCursor = posts.Count > 0 ? posts.Last().Id.ToString() : null;

        return new CursorPagedList<PostDto>(posts, nextCursor, hasNextPage);
    }
}
