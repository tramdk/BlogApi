using FloraCore.Application.Products.DTOs;
using FloraCore.Domain.Entities;

namespace FloraCore.Application.Interfaces;

/// <summary>
/// Interface for product repository.
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// Gets all products.
    /// </summary>
    /// <returns>A list of products.</returns>
    Task<List<Product>> GetAllAsync();

    /// <summary>
    /// Gets a product by id.
    /// </summary>
    /// <param name="id">The id of the product.</param>
    /// <returns>A product.</returns>
    Task<Product?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a product.
    /// </summary>
    /// <param name="product">The product to create.</param>
    /// <returns>The created product.</returns>
    Task<Product> CreateAsync(Product product);

    /// <summary>
    /// Updates a product.
    /// </summary>
    /// <param name="product">The product to update.</param>
    /// <returns>The updated product.</returns>
    Task<Product> UpdateAsync(Product product);

    /// <summary>
    /// Deletes a product.
    /// </summary>
    /// <param name="id">The id of the product.</param>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Searches products based on the search term.
    /// </summary>
    /// <param name="searchTerm">The search term.</param>
    /// <returns>A list of product search results.</returns>
    Task<List<ProductSearchResultDto>> SearchProductsAsync(string searchTerm);
}
