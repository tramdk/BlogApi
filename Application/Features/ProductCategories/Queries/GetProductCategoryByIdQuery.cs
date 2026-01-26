using BlogApi.Domain.Entities;
using BlogApi.Application.Common.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlogApi.Application.Features.ProductCategories.Queries;

public record GetProductCategoryByIdQuery(Guid Id) : IRequest<ProductCategoryDto?>;

public class GetProductCategoryByIdQueryHandler : IRequestHandler<GetProductCategoryByIdQuery, ProductCategoryDto?>
{
    private readonly IGenericRepository<ProductCategory, Guid> _repository;

    public GetProductCategoryByIdQueryHandler(IGenericRepository<ProductCategory, Guid> repository)
    {
        _repository = repository;
    }

    public async Task<ProductCategoryDto?> Handle(GetProductCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var c = await _repository.GetByIdAsync(request.Id);
        if (c == null) return null;

        return new ProductCategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            ImageUrl = c.ImageUrl,
            CreatedAt = c.CreatedAt
        };
    }
}
