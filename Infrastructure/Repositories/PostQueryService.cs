using BlogApi.Application.Common.Interfaces;
using BlogApi.Application.Common.Models;
using BlogApi.Application.Features.Posts.DTOs;
using BlogApi.Infrastructure.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace BlogApi.Infrastructure.Repositories;

public class PostQueryService : IPostQueryService
{
    private readonly AppDbContext _context;

    public PostQueryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CursorPagedList<PostDto>> GetPostsAsync(Guid? cursor, int pageSize)
    {
        using var connection = _context.Database.GetDbConnection();
        
        var isSqlServer = _context.Database.IsSqlServer();
        
        string sql;
        if (isSqlServer)
        {
            sql = @"
                SELECT TOP (@PageSizePlusOne) 
                    p.Id, 
                    p.Title, 
                    u.FullName as AuthorName, 
                    p.AverageRating, 
                    p.CreatedAt,
                    p.CategoryId
                FROM Posts p
                INNER JOIN AspNetUsers u ON p.AuthorId = u.Id
                WHERE (@Cursor IS NULL OR p.Id < @Cursor)
                ORDER BY p.Id DESC";
        }
        else // SQLite for testing
        {
            sql = @"
                SELECT 
                    p.Id, 
                    p.Title, 
                    u.FullName as AuthorName, 
                    p.AverageRating, 
                    p.CreatedAt,
                    p.CategoryId
                FROM Posts p
                INNER JOIN AspNetUsers u ON p.AuthorId = u.Id
                WHERE (@Cursor IS NULL OR p.Id < @Cursor)
                ORDER BY p.Id DESC
                LIMIT @PageSizePlusOne";
        }

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
