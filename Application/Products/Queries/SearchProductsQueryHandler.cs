using FloraCore.Application.Interfaces;
using FloraCore.Application.Products.DTOs;
using MediatR;

namespace FloraCore.Application.Products.Queries;

/// <summary>
/// Handler for the <see cref="SearchProductsQuery"/>.
/// </summary>
public class SearchProductsQueryHandler : IRequestHandler<SearchProductsQuery, List<ProductSearchResultDto>>
{
    private readonly IProductRepository _productRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchProductsQueryHandler"/> class.
    /// </summary>
    /// <param name="productRepository">The product repository.</param>
    public SearchProductsQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
    }

    /// <inheritdoc />
    public async Task<List<ProductSearchResultDto>> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
    {
        return await _productRepository.SearchProductsAsync(request.SearchTerm);
    }
}
