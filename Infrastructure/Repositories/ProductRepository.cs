using FloraCore.Application.Interfaces;
using FloraCore.Application.Features.Products.DTOs;
using FloraCore.Domain.Entities;
using FloraCore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FloraCore.Infrastructure.Repositories;

/// <summary>
/// Implementation of the product repository inheriting from <see cref="GenericRepository{TEntity, TKey}"/>.
/// </summary>
public class ProductRepository(AppDbContext context) : GenericRepository<Product, Guid>(context ?? throw new ArgumentNullException(nameof(context))), IProductRepository
{

    /// <inheritdoc />
    public async Task<List<ProductSearchResultDto>> SearchProductsAsync(string searchTerm)
    {
        return await _context.Products
            .Where(p => p.Name.Contains(searchTerm) || (p.Description != null && p.Description.Contains(searchTerm)))
            .Select(p => new ProductSearchResultDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Stock = p.Stock,
                ImageUrl = p.ImageUrl,
                AverageRating = p.AverageRating
            })
            .ToListAsync();
    }
}
