using FloraCore.Application.Common.Models;
using System.Linq.Expressions;

namespace FloraCore.Application.Common.Interfaces;

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
    /// Get all entities without any filtering or pagination.
    /// </summary>
    /// <remarks>
    /// WARNING: This loads ALL records into memory. For large tables, use
    /// <see cref="GetWithOptionsAsync"/> with pagination instead.
    /// </remarks>
    [Obsolete("Avoid using GetAllAsync() in production — it loads the entire table. Use GetWithOptionsAsync() with pagination.")]
    Task<IEnumerable<TEntity>> GetAllAsync();
    
    /// <summary>
    /// Find entities by predicate
    /// </summary>
    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);
    
    /// <summary>
    /// Add new entity and immediately persist to database.
    /// </summary>
    Task AddAsync(TEntity entity);

    /// <summary>
    /// Stage a new entity for insertion WITHOUT calling SaveChanges.
    /// Use with IUnitOfWork for atomic multi-step operations.
    /// </summary>
    Task StageAddAsync(TEntity entity);

    /// <summary>
    /// Stage multiple entities for insertion WITHOUT calling SaveChanges.
    /// </summary>
    Task StageAddRangeAsync(IEnumerable<TEntity> entities);

    /// <summary>
    /// Update existing entity and immediately persist to database.
    /// </summary>
    Task UpdateAsync(TEntity entity);

    /// <summary>
    /// Stage an update WITHOUT calling SaveChanges.
    /// Use with IUnitOfWork for atomic multi-step operations.
    /// </summary>
    void StageUpdate(TEntity entity);

    /// <summary>
    /// Delete entity and immediately persist to database.
    /// </summary>
    Task DeleteAsync(TEntity entity);

    /// <summary>
    /// Delete entity by its ID and immediately persist to database.
    /// </summary>
    Task DeleteAsync(TKey id);

    /// <summary>
    /// Stage a deletion WITHOUT calling SaveChanges.
    /// Use with IUnitOfWork for atomic multi-step operations.
    /// </summary>
    void StageDelete(TEntity entity);

    /// <summary>
    /// Stage a deletion of entity by its ID WITHOUT calling SaveChanges.
    /// Use with IUnitOfWork for atomic multi-step operations.
    /// </summary>
    void StageDelete(TKey id);
    
    /// <summary>
    /// Get queryable for custom queries
    /// </summary>
    IQueryable<TEntity> GetQueryable();
    
    /// <summary>
    /// Get queryable with advanced query options applied
    /// </summary>
    IQueryable<TEntity> GetQueryable(QueryOptions<TEntity> options);
    
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
