using FloraCore.Application.Common.Interfaces;

namespace FloraCore.Infrastructure.Repositories;

/// <summary>
/// SQL Server implementation for IPostQueryDialect.
/// </summary>
public class SqlServerPostQueryDialect : IPostQueryDialect
{
    public string GetPostsSql()
    {
        return @"
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
}

/// <summary>
/// PostgreSQL implementation for IPostQueryDialect.
/// </summary>
public class PostgresPostQueryDialect : IPostQueryDialect
{
    public string GetPostsSql()
    {
        return @"
            SELECT 
                p.""Id"", 
                p.""Title"", 
                u.""FullName"" as ""AuthorName"", 
                p.""AverageRating"", 
                p.""CreatedAt"",
                p.""CategoryId""
            FROM ""Posts"" p
            INNER JOIN ""AspNetUsers"" u ON p.""AuthorId"" = u.""Id""
            WHERE (@Cursor IS NULL OR p.""Id"" < @Cursor)
            ORDER BY p.""Id"" DESC
            LIMIT @PageSizePlusOne";
    }
}

/// <summary>
/// SQLite/Default implementation for IPostQueryDialect.
/// </summary>
public class SqlitePostQueryDialect : IPostQueryDialect
{
    public string GetPostsSql()
    {
        return @"
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
}
