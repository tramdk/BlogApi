using FloraCore.Application.Common.Interfaces;
using FloraCore.Application.Products.DTOs;
using FloraCore.Domain.Entities;

namespace FloraCore.Application.Interfaces;

/// <summary>
/// Interface for product repository, inheriting common CRUD operations from IGenericRepository.
/// </summary>
public interface IProductRepository : IGenericRepository<Product, Guid>
{
    /// <summary>
    /// Searches products based on the search term.
    /// </summary>
    /// <param name="searchTerm">The search term.</param>
    /// <returns>A list of product search results.</returns>
    Task<List<ProductSearchResultDto>> SearchProductsAsync(string searchTerm);
}
