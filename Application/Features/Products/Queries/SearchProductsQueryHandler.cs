using FloraCore.Application.Interfaces;
using MediatR;
using FloraCore.Application.Features.Products.DTOs;
using System;

namespace FloraCore.Application.Features.Products.Queries;

/// <summary>
/// Handler for the <see cref="SearchProductsQuery"/>.
/// </summary>
/// <param name="productRepository">The product repository.</param>
public class SearchProductsQueryHandler(IProductRepository productRepository) : IRequestHandler<SearchProductsQuery, List<ProductSearchResultDto>>
{
    private readonly IProductRepository _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));

    /// <inheritdoc />
    public async Task<List<ProductSearchResultDto>> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
    {
        return await _productRepository.SearchProductsAsync(request.SearchTerm);
    }
}
