using BlogApi.Application.Features.Products.Queries;
using BlogApi.Domain.Entities;
using BlogApi.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlogApi.Application.Features.Products.Queries;

public record GetProductsQuery : IRequest<IEnumerable<ProductDto>>;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, IEnumerable<ProductDto>>
{
    private readonly IGenericRepository<Product, Guid> _productRepository;

    public GetProductsQueryHandler(IGenericRepository<Product, Guid> productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<IEnumerable<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _productRepository.GetQueryable()
            .Include(p => p.Category)
            .ToListAsync(cancellationToken);

        return products.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            Stock = p.Stock,
            ImageUrl = p.ImageUrl,
            AverageRating = p.AverageRating,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name
        });
    }
}
