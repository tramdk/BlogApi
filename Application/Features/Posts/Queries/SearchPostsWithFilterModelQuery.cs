using BlogApi.Application.Common.Helpers;
using BlogApi.Application.Common.Interfaces;
using BlogApi.Application.Common.Models;
using BlogApi.Application.Features.Posts.DTOs;
using BlogApi.Domain.Entities;
using MediatR;

namespace BlogApi.Application.Features.Posts.Queries;

/// <summary>
/// Query with FilterModel (AG-Grid, MUI DataGrid style)
/// </summary>
public record SearchPostsWithFilterModelQuery(FilterModel FilterModel) 
    : IRequest<PagedResult<PostDto>>;

public class SearchPostsWithFilterModelHandler 
    : IRequestHandler<SearchPostsWithFilterModelQuery, PagedResult<PostDto>>
{
    private readonly IGenericRepository<Post, Guid> _repository;
    
    public SearchPostsWithFilterModelHandler(IGenericRepository<Post, Guid> repository)
    {
        _repository = repository;
    }
    
    public async Task<PagedResult<PostDto>> Handle(
        SearchPostsWithFilterModelQuery request, 
        CancellationToken ct)
    {
        var filterModel = request.FilterModel;
        
        // Parse filter model to expression
        var filter = FilterModelParser.ParseFilter<Post>(filterModel);
        
        // Build query options
        var optionsBuilder = new QueryOptionsBuilder<Post>()
            .WithInclude(p => p.Author!)
            .AsNoTracking();
        
        // Apply filter
        if (filter != null)
        {
            optionsBuilder.WithFilter(filter);
        }
        
        // Apply sorting
        if (filterModel.Sort != null && filterModel.Sort.Any())
        {
            var options = optionsBuilder.Build();
            FilterModelParser.ApplySorting(options, filterModel.Sort);
            optionsBuilder = new QueryOptionsBuilder<Post>();
            
            // Rebuild with sorting
            if (filter != null)
                optionsBuilder.WithFilter(filter);
            
            optionsBuilder
                .WithInclude(p => p.Author!)
                .AsNoTracking();
            
            if (options.OrderBy != null)
                optionsBuilder.WithOrderBy(options.OrderBy);
            else if (options.OrderByDescending != null)
                optionsBuilder.WithOrderByDescending(options.OrderByDescending);
        }
        
        // Apply pagination
        var skip = filterModel.Page * filterModel.PageSize;
        optionsBuilder.WithPagination(skip, filterModel.PageSize);
        
        var queryOptions = optionsBuilder.Build();
        
        // Execute query
        var pagedResult = await _repository.GetPagedAsync(queryOptions);
        
        // Map to DTOs
        return new PagedResult<PostDto>(
            pagedResult.Items.Select(p => new PostDto(
                p.Id,
                p.Title,
                p.Author?.FullName ?? "Unknown",
                p.AverageRating,
                p.CreatedAt,
                p.CategoryId
            )).ToList(),
            pagedResult.TotalCount,
            pagedResult.PageNumber,
            pagedResult.PageSize
        );
    }
}
