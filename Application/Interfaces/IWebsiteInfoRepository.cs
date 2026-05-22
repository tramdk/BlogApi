using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FloraCore.Domain.Entities;

namespace FloraCore.Application.Interfaces;

public interface IWebsiteInfoRepository
{
    Task<WebsiteInfo?> GetByIdAsync(Guid id);
    Task<IEnumerable<WebsiteInfo>> GetAllAsync();
    Task<Guid> AddAsync(WebsiteInfo websiteInfo);
    Task UpdateAsync(WebsiteInfo websiteInfo);
    Task DeleteAsync(Guid id);
}