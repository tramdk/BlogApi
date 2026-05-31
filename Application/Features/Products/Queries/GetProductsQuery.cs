using FloraCore.Application.Common.Interfaces;
using FloraCore.Application.Common.Models;
using FloraCore.Domain.Entities;
using MediatR;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using FloraCore.Application.Features.Products.DTOs;

namespace FloraCore.Application.Features.Products.Queries;

// Note: Optional parameters do not require ArgumentNullException.ThrowIfNull
public record GetProductsQuery(int PageNumber = 1, int PageSize = 10, string? SearchTerm = null) : IRequest<PagedResult<ProductDto>>;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    private readonly IGenericRepository<Product, Guid> _productRepository;
    private readonly IMapper _mapper;

    public GetProductsQueryHandler(IGenericRepository<Product, Guid> productRepository, IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var optionsBuilder = new QueryOptionsBuilder<Product>()
            .WithPagination((request.PageNumber - 1) * request.PageSize, request.PageSize)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            optionsBuilder.WithFilter(p => p.Name.ToLower().Contains(searchTerm) || p.Description.ToLower().Contains(searchTerm));
        }

        var queryOptions = optionsBuilder.Build();
        
        var count = await _productRepository.CountAsync(queryOptions.Filter);
        
        var items = await _productRepository.GetQueryable(queryOptions)
            .ProjectTo<ProductDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductDto>(items, count, request.PageNumber, request.PageSize);
    }
}
