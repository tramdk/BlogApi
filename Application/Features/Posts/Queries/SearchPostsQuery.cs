using BlogApi.Application.Common.Extensions;
using BlogApi.Application.Common.Interfaces;
using BlogApi.Application.Common.Models;
using BlogApi.Application.Features.Posts.DTOs;
using BlogApi.Domain.Entities;
using MediatR;
using System.Linq.Expressions;

namespace BlogApi.Application.Features.Posts.Queries;

/// <summary>
/// Advanced search query with filtering, sorting, and pagination
/// </summary>
public record SearchPostsQuery(
    string? SearchTerm = null,
    string? CategoryId = null,
    double? MinRating = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string SortBy = "CreatedAt",
    bool SortDescending = true,
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<PagedResult<PostDto>>;

public class SearchPostsHandler : IRequestHandler<SearchPostsQuery, PagedResult<PostDto>>
{
    private readonly IGenericRepository<Post, Guid> _repository;
    
    public SearchPostsHandler(IGenericRepository<Post, Guid> repository)
    {
        _repository = repository;
    }
    
    public async Task<PagedResult<PostDto>> Handle(SearchPostsQuery request, CancellationToken ct)
    {
        // Build complex filter using expression extensions
        Expression<Func<Post, bool>> filter = p => true; // Start with always true
        
        // Search term filter
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            filter = filter.And(p => 
                p.Title.ToLower().Contains(searchTerm) || 
                p.Content.ToLower().Contains(searchTerm));
        }
        
        // Category filter
        if (!string.IsNullOrEmpty(request.CategoryId))
        {
            filter = filter.And(p => p.CategoryId == request.CategoryId);
        }
        
        // Rating filter
        if (request.MinRating.HasValue)
        {
            filter = filter.And(p => p.AverageRating >= request.MinRating.Value);
        }
        
        // Date range filter
        if (request.FromDate.HasValue)
        {
            filter = filter.And(p => p.CreatedAt >= request.FromDate.Value);
        }
        
        if (request.ToDate.HasValue)
        {
            filter = filter.And(p => p.CreatedAt <= request.ToDate.Value);
        }
        
        // Build query options with fluent builder
        var optionsBuilder = new QueryOptionsBuilder<Post>()
            .WithFilter(filter)
            .WithInclude(p => p.Author!)
            .WithPagination((request.PageNumber - 1) * request.PageSize, request.PageSize)
            .AsNoTracking();
        
        // Dynamic sorting
        switch (request.SortBy.ToLower())
        {
            case "title":
                if (request.SortDescending)
                    optionsBuilder.WithOrderByDescending(p => p.Title);
                else
                    optionsBuilder.WithOrderBy(p => p.Title);
                break;
                
            case "rating":
                if (request.SortDescending)
                    optionsBuilder.WithOrderByDescending(p => p.AverageRating);
                else
                    optionsBuilder.WithOrderBy(p => p.AverageRating);
                break;
                
            case "createdat":
            default:
                if (request.SortDescending)
                    optionsBuilder.WithOrderByDescending(p => p.CreatedAt);
                else
                    optionsBuilder.WithOrderBy(p => p.CreatedAt);
                break;
        }
        
        var options = optionsBuilder.Build();
        
        // Execute query
        var pagedResult = await _repository.GetPagedAsync(options);
        
        // Map to DTOs
        return new PagedResult<PostDto>(
            pagedResult.Items.Select(p => new PostDto(
                p.Id,
                p.Title,
                p.Author.FullName,
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
