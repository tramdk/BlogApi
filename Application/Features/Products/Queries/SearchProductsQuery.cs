using MediatR;
using FloraCore.Application.Features.Products.DTOs;

namespace FloraCore.Application.Features.Products.Queries;

/// <summary>
/// Represents a query to search for products.
/// </summary>
public record SearchProductsQuery(string SearchTerm) : IRequest<List<ProductSearchResultDto>>;
