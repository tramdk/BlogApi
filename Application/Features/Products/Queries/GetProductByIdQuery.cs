using BlogApi.Application.Features.Products.Queries;
using BlogApi.Domain.Entities;
using BlogApi.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlogApi.Application.Features.Products.Queries;

public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto?>;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly IGenericRepository<Product, Guid> _productRepository;

    public GetProductByIdQueryHandler(IGenericRepository<Product, Guid> productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var p = await _productRepository.GetQueryable()
            .Include(p => p.Category)
            .Include(p => p.Reviews)
            .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
            
        if (p == null) return null;

        return new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            Stock = p.Stock,
            ImageUrl = p.ImageUrl,
            AverageRating = p.AverageRating,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name,
            Reviews = p.Reviews.Select(r => new ReviewDto
            {
                Id = r.Id,
                UserName = r.User.FullName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            }).ToList()
        };
    }
}
