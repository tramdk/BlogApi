using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FloraCore.Application.Interfaces;
using FloraCore.Domain.Entities;
using FloraCore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FloraCore.Infrastructure.Repositories;

public class WebsiteInfoRepository : IWebsiteInfoRepository
{
    private readonly AppDbContext _context;

    public WebsiteInfoRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<WebsiteInfo?> GetByIdAsync(Guid id)
    {
        return await _context.WebsiteInfos.FindAsync(id);
    }

    public async Task<IEnumerable<WebsiteInfo>> GetAllAsync()
    {
        return await _context.WebsiteInfos.ToListAsync();
    }

    public async Task<Guid> AddAsync(WebsiteInfo websiteInfo)
    {
        ArgumentNullException.ThrowIfNull(websiteInfo);
        await _context.WebsiteInfos.AddAsync(websiteInfo);
        await _context.SaveChangesAsync();
        return websiteInfo.Id;
    }

    public async Task UpdateAsync(WebsiteInfo websiteInfo)
    {
        ArgumentNullException.ThrowIfNull(websiteInfo);
        _context.WebsiteInfos.Update(websiteInfo);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _context.WebsiteInfos.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}