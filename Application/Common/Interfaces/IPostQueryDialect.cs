namespace FloraCore.Application.Common.Interfaces;

/// <summary>
/// Strategy interface to abstract SQL dialect specific to database engines (PostgreSQL, SQL Server, SQLite).
/// Follows Strategy and Provider patterns.
/// </summary>
public interface IPostQueryDialect
{
    /// <summary>
    /// Gets the SQL query for retrieving posts with cursor-based pagination.
    /// </summary>
    string GetPostsSql();
}
