using System;
using FloraCore.Application.Common.Interfaces;
using FloraCore.Domain.Entities;

namespace FloraCore.Application.Interfaces;

/// <summary>
/// Interface for WebsiteInfo repository, inheriting common CRUD operations from IGenericRepository.
/// </summary>
public interface IWebsiteInfoRepository : IGenericRepository<WebsiteInfo, Guid>
{
}