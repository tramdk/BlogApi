using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FloraCore.Application.Interfaces;
using FloraCore.Domain.Entities;
using FloraCore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FloraCore.Infrastructure.Repositories;

/// <summary>
/// Implementation of <see cref="IWebsiteInfoRepository"/> for managing website information in the database.
/// </summary>
public class WebsiteInfoRepository : IWebsiteInfoRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<WebsiteInfoRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebsiteInfoRepository"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="logger">The logger.</param>
    public WebsiteInfoRepository(AppDbContext context, ILogger<WebsiteInfoRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets website information by ID.
    /// </summary>
    /// <param name="id">The ID of the website information.</param>
    /// <returns>The website information, or null if not found.</returns>
    public async Task<WebsiteInfo?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _context.WebsiteInfos.FindAsync(id);
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
    public async Task<IEnumerable<WebsiteInfo>> GetAllAsync()
    {
        try
        {
            return await _context.WebsiteInfos.ToListAsync();
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
    /// <returns>The ID of the newly added website information.</returns>
    public async Task<Guid> AddAsync(WebsiteInfo websiteInfo)
    {
        ArgumentNullException.ThrowIfNull(websiteInfo);
        try
        {
            await _context.WebsiteInfos.AddAsync(websiteInfo);
            await _context.SaveChangesAsync();
            return websiteInfo.Id;
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
    public async Task UpdateAsync(WebsiteInfo websiteInfo)
    {
        ArgumentNullException.ThrowIfNull(websiteInfo);
        try
        {
            _context.WebsiteInfos.Update(websiteInfo);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating WebsiteInfo with ID: {Id}", websiteInfo.Id);
            throw;
        }
    }

    /// <summary>
    /// Deletes website information from the database.
    /// </summary>
    /// <param name="id">The ID of the website information to delete.</param>
    public async Task DeleteAsync(Guid id)
    {
        try
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _context.WebsiteInfos.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting WebsiteInfo with ID: {Id}", id);
            throw;
        }
    }
}
