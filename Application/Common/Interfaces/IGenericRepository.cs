using BlogApi.Application.Common.Models;
using System.Linq.Expressions;

namespace BlogApi.Application.Common.Interfaces;

/// <summary>
/// Generic repository interface for common CRUD operations.
/// This interface is defined in the Application layer to follow Clean Architecture principles.
/// </summary>
public interface IGenericRepository<TEntity, TKey> where TEntity : class
{
    // ========== Basic CRUD Operations ==========
    
    /// <summary>
    /// Get entity by ID
    /// </summary>
    Task<TEntity?> GetByIdAsync(TKey id);
    
    /// <summary>
    /// Get all entities
    /// </summary>
    Task<IEnumerable<TEntity>> GetAllAsync();
    
    /// <summary>
    /// Find entities by predicate
    /// </summary>
    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);
    
    /// <summary>
    /// Add new entity
    /// </summary>
    Task AddAsync(TEntity entity);
    
    /// <summary>
    /// Update existing entity
    /// </summary>
    Task UpdateAsync(TEntity entity);
    
    /// <summary>
    /// Delete entity
    /// </summary>
    Task DeleteAsync(TEntity entity);
    
    /// <summary>
    /// Get queryable for custom queries
    /// </summary>
    IQueryable<TEntity> GetQueryable();
    
    // ========== Advanced Query Operations ==========
    
    /// <summary>
    /// Get entities with advanced query options (filter, sort, pagination, includes)
    /// </summary>
    Task<IEnumerable<TEntity>> GetWithOptionsAsync(QueryOptions<TEntity> options);
    
    /// <summary>
    /// Get single entity with query options
    /// </summary>
    Task<TEntity?> GetSingleWithOptionsAsync(QueryOptions<TEntity> options);
    
    /// <summary>
    /// Get paged result with query options
    /// </summary>
    Task<PagedResult<TEntity>> GetPagedAsync(QueryOptions<TEntity> options);
    
    /// <summary>
    /// Count entities matching filter
    /// </summary>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? filter = null);
    
    /// <summary>
    /// Check if any entity matches filter
    /// </summary>
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> filter);
    
    /// <summary>
    /// Get first entity matching filter or null
    /// </summary>
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> filter);
}
