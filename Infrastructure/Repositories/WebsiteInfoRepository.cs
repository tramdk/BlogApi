using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FloraCore.Application.Interfaces;
using FloraCore.Domain.Entities;
using FloraCore.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace FloraCore.Infrastructure.Repositories;

/// <summary>
/// Implementation of <see cref="IWebsiteInfoRepository"/> inheriting from <see cref="GenericRepository{TEntity, TKey}"/>.
/// Overrides base CRUD methods to preserve custom logging behavior.
/// </summary>
public class WebsiteInfoRepository(AppDbContext context, ILogger<WebsiteInfoRepository> logger) 
    : GenericRepository<WebsiteInfo, Guid>(context), IWebsiteInfoRepository
{
    private readonly ILogger<WebsiteInfoRepository> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Gets website information by ID.
    /// </summary>
    /// <param name="id">The ID of the website information.</param>
    /// <returns>The website information, or null if not found.</returns>
    public override async Task<WebsiteInfo?> GetByIdAsync(Guid id)
    {
        try
        {
            return await base.GetByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting WebsiteInfo with ID: {Id}", id);
            return null;
        }
    }

    /// <summary>
    /// Gets all website information.
    /// </summary>
    /// <returns>A list of website information.</returns>
    [Obsolete("Avoid using GetAllAsync() in production — it loads the entire table. Use GetWithOptionsAsync() with pagination.")]
    public override async Task<IEnumerable<WebsiteInfo>> GetAllAsync()
    {
        try
        {
            return await base.GetAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all WebsiteInfos.");
            return new List<WebsiteInfo>();
        }
    }

    /// <summary>
    /// Adds new website information to the database.
    /// </summary>
    /// <param name="websiteInfo">The website information to add.</param>
    public override async Task AddAsync(WebsiteInfo websiteInfo)
    {
        ArgumentNullException.ThrowIfNull(websiteInfo);
        try
        {
            await base.AddAsync(websiteInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding WebsiteInfo.");
            throw;
        }
    }

    /// <summary>
    /// Updates existing website information in the database.
    /// </summary>
    /// <param name="websiteInfo">The website information to update.</param>
    public override async Task UpdateAsync(WebsiteInfo websiteInfo)
    {
        ArgumentNullException.ThrowIfNull(websiteInfo);
        try
        {
            await base.UpdateAsync(websiteInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating WebsiteInfo with ID: {Id}", websiteInfo.Id);
            throw;
        }
    }

    /// <summary>
    /// Deletes website information from the database by ID.
    /// </summary>
    /// <param name="id">The ID of the website information to delete.</param>
    public override async Task DeleteAsync(Guid id)
    {
        try
        {
            await base.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting WebsiteInfo with ID: {Id}", id);
            throw;
        }
    }
}
