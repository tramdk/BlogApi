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

namespace FloraCore.Infrastructure.Repositories;

/// <summary>
/// Implementation of IPostQueryService using Dapper for optimal read performance with cursor pagination.
/// Utilizes the Strategy Pattern (IPostQueryDialect) to support multiple database dialects safely.
/// </summary>
public class PostQueryService(AppDbContext context, IPostQueryDialect dialect) : IPostQueryService
{
    private readonly AppDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly IPostQueryDialect _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));

    public async Task<CursorPagedList<PostDto>> GetPostsAsync(Guid? cursor, int pageSize)
    {
        using var connection = _context.Database.GetDbConnection();
        
        // Retrieve the dialect-specific SQL string
        var sql = _dialect.GetPostsSql();

        var parameters = new { Cursor = cursor, PageSizePlusOne = pageSize + 1 };
        
        var posts = (await connection.QueryAsync<PostDto>(sql, parameters)).ToList();

        var hasNextPage = posts.Count > pageSize;
        if (hasNextPage)
        {
            posts.RemoveAt(pageSize);
        }

        var nextCursor = posts.Count > 0 ? posts.Last().Id.ToString() : null;

        return new CursorPagedList<PostDto>(posts, nextCursor, hasNextPage);
    }
}

