using MediatR;
using FloraCore.Application.Products.DTOs;

namespace FloraCore.Application.Products.Queries;

/// <summary>
/// Represents a query to search for products.
/// </summary>
public record SearchProductsQuery(string SearchTerm) : IRequest<List<ProductSearchResultDto>>;
