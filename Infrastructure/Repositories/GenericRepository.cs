using BlogApi.Application.Common.Interfaces;
using BlogApi.Application.Common.Models;
using BlogApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BlogApi.Infrastructure.Repositories;

/// <summary>
/// Generic repository implementation.
/// This class implements the IGenericRepository interface defined in the Application layer.
/// </summary>
public class GenericRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey> where TEntity : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public GenericRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    // ========== Basic CRUD Operations ==========

    public virtual async Task<TEntity?> GetByIdAsync(TKey id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public virtual async Task AddAsync(TEntity entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public virtual async Task UpdateAsync(TEntity entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    public virtual async Task DeleteAsync(TEntity entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public IQueryable<TEntity> GetQueryable()
    {
        return _dbSet.AsQueryable();
    }

    // ========== Advanced Query Operations ==========

    public virtual async Task<IEnumerable<TEntity>> GetWithOptionsAsync(QueryOptions<TEntity> options)
    {
        var query = ApplyQueryOptions(_dbSet.AsQueryable(), options);
        return await query.ToListAsync();
    }

    public virtual async Task<TEntity?> GetSingleWithOptionsAsync(QueryOptions<TEntity> options)
    {
        var query = ApplyQueryOptions(_dbSet.AsQueryable(), options);
        return await query.FirstOrDefaultAsync();
    }

    public virtual async Task<PagedResult<TEntity>> GetPagedAsync(QueryOptions<TEntity> options)
    {
        var query = _dbSet.AsQueryable();

        // Apply filter
        if (options.Filter != null)
        {
            query = query.Where(options.Filter);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply includes, sorting, and pagination
        query = ApplyQueryOptions(query, options);

        var items = await query.ToListAsync();

        var pageNumber = options.Skip.HasValue && options.Take.HasValue
            ? (options.Skip.Value / options.Take.Value) + 1
            : 1;

        var pageSize = options.Take ?? totalCount;

        return new PagedResult<TEntity>(items, totalCount, pageNumber, pageSize);
    }

    public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>>? filter = null)
    {
        if (filter == null)
        {
            return await _dbSet.CountAsync();
        }

        return await _dbSet.CountAsync(filter);
    }

    public virtual async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> filter)
    {
        return await _dbSet.AnyAsync(filter);
    }

    public virtual async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> filter)
    {
        return await _dbSet.FirstOrDefaultAsync(filter);
    }

    // ========== Helper Methods ==========

    /// <summary>
    /// Apply query options to IQueryable
    /// </summary>
    protected virtual IQueryable<TEntity> ApplyQueryOptions(IQueryable<TEntity> query, QueryOptions<TEntity> options)
    {
        // Apply filter
        if (options.Filter != null)
        {
            query = query.Where(options.Filter);
        }

        // Apply includes
        foreach (var include in options.Includes)
        {
            query = query.Include(include);
        }

        foreach (var includeString in options.IncludeStrings)
        {
            query = query.Include(includeString);
        }

        // Apply sorting
        if (options.OrderBy != null)
        {
            query = query.OrderBy(options.OrderBy);
        }
        else if (options.OrderByDescending != null)
        {
            query = query.OrderByDescending(options.OrderByDescending);
        }

        // Apply no tracking
        if (options.AsNoTracking)
        {
            query = query.AsNoTracking();
        }

        // Apply split query
        if (options.AsSplitQuery)
        {
            query = query.AsSplitQuery();
        }

        // Apply pagination
        if (options.Skip.HasValue)
        {
            query = query.Skip(options.Skip.Value);
        }

        if (options.Take.HasValue)
        {
            query = query.Take(options.Take.Value);
        }

        return query;
    }
}
