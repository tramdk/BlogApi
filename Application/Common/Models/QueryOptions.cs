using System.Linq.Expressions;

namespace BlogApi.Application.Common.Models;

/// <summary>
/// Options for querying entities with filtering, sorting, and pagination.
/// </summary>
public class QueryOptions<TEntity> where TEntity : class
{
    /// <summary>
    /// Filter predicate (WHERE clause)
    /// </summary>
    public Expression<Func<TEntity, bool>>? Filter { get; set; }
    
    /// <summary>
    /// Order by expression
    /// </summary>
    public Expression<Func<TEntity, object>>? OrderBy { get; set; }
    
    /// <summary>
    /// Order by descending expression
    /// </summary>
    public Expression<Func<TEntity, object>>? OrderByDescending { get; set; }
    
    /// <summary>
    /// Include navigation properties
    /// </summary>
    public List<Expression<Func<TEntity, object>>> Includes { get; set; } = new();
    
    /// <summary>
    /// Include string-based navigation properties (for nested includes)
    /// </summary>
    public List<string> IncludeStrings { get; set; } = new();
    
    /// <summary>
    /// Skip count for pagination
    /// </summary>
    public int? Skip { get; set; }
    
    /// <summary>
    /// Take count for pagination
    /// </summary>
    public int? Take { get; set; }
    
    /// <summary>
    /// Disable tracking for read-only queries
    /// </summary>
    public bool AsNoTracking { get; set; } = false;
    
    /// <summary>
    /// Use split query for multiple includes
    /// </summary>
    public bool AsSplitQuery { get; set; } = false;
}

/// <summary>
/// Fluent builder for QueryOptions
/// </summary>
public class QueryOptionsBuilder<TEntity> where TEntity : class
{
    private readonly QueryOptions<TEntity> _options = new();
    
    public QueryOptionsBuilder<TEntity> WithFilter(Expression<Func<TEntity, bool>> filter)
    {
        _options.Filter = filter;
        return this;
    }
    
    public QueryOptionsBuilder<TEntity> WithOrderBy(Expression<Func<TEntity, object>> orderBy)
    {
        _options.OrderBy = orderBy;
        return this;
    }
    
    public QueryOptionsBuilder<TEntity> WithOrderByDescending(Expression<Func<TEntity, object>> orderByDescending)
    {
        _options.OrderByDescending = orderByDescending;
        return this;
    }
    
    public QueryOptionsBuilder<TEntity> WithInclude(Expression<Func<TEntity, object>> include)
    {
        _options.Includes.Add(include);
        return this;
    }
    
    public QueryOptionsBuilder<TEntity> WithInclude(string includeString)
    {
        _options.IncludeStrings.Add(includeString);
        return this;
    }
    
    public QueryOptionsBuilder<TEntity> WithPagination(int skip, int take)
    {
        _options.Skip = skip;
        _options.Take = take;
        return this;
    }
    
    public QueryOptionsBuilder<TEntity> AsNoTracking()
    {
        _options.AsNoTracking = true;
        return this;
    }
    
    public QueryOptionsBuilder<TEntity> AsSplitQuery()
    {
        _options.AsSplitQuery = true;
        return this;
    }
    
    public QueryOptions<TEntity> Build() => _options;
}
