using BlogApi.Application.Common.Extensions;
using BlogApi.Application.Common.Helpers;
using BlogApi.Application.Common.Interfaces;
using BlogApi.Application.Common.Models;
using BlogApi.Application.Features.Posts.DTOs;
using BlogApi.Domain.Entities;
using MediatR;
using System.Linq.Expressions;

namespace BlogApi.Application.Features.Posts.Queries;

/// <summary>
/// Unified search query that supports multiple approaches:
/// 1. Simple parameters (searchTerm, categoryId, etc.)
/// 2. FilterModel (AG-Grid, MUI DataGrid style)
/// 3. Mixed approach
/// </summary>
public record UnifiedSearchPostsQuery(UnifiedSearchRequest Request) 
    : IRequest<PagedResult<PostDto>>;

public class UnifiedSearchPostsHandler 
    : IRequestHandler<UnifiedSearchPostsQuery, PagedResult<PostDto>>
{
    private readonly IGenericRepository<Post, Guid> _repository;
    
    public UnifiedSearchPostsHandler(IGenericRepository<Post, Guid> repository)
    {
        _repository = repository;
    }
    
    public async Task<PagedResult<PostDto>> Handle(
        UnifiedSearchPostsQuery request, 
        CancellationToken ct)
    {
        var searchRequest = request.Request;
        
        // Build filter expression
        Expression<Func<Post, bool>>? filter = null;
        
        if (searchRequest.IsFilterModelRequest)
        {
            // Use FilterModel approach
            var filterModel = new FilterModel
            {
                Filters = searchRequest.Filters!,
                Sort = searchRequest.Sort ?? new List<SortModel>(),
                Page = searchRequest.GetEffectivePage(),
                PageSize = searchRequest.PageSize
            };
            
            filter = FilterModelParser.ParseFilter<Post>(filterModel);
        }
        else if (searchRequest.IsSimpleSearchRequest)
        {
            // Use simple search approach
            filter = BuildSimpleFilter(searchRequest);
        }
        
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
        ApplySorting(optionsBuilder, searchRequest);
        
        // Apply pagination
        var skip = searchRequest.GetEffectivePage() * searchRequest.PageSize;
        optionsBuilder.WithPagination(skip, searchRequest.PageSize);
        
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
    
    /// <summary>
    /// Build filter from simple search parameters
    /// </summary>
    private Expression<Func<Post, bool>>? BuildSimpleFilter(UnifiedSearchRequest request)
    {
        Expression<Func<Post, bool>>? filter = null;
        
        // Search term filter
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            Expression<Func<Post, bool>> searchFilter = p => 
                p.Title.ToLower().Contains(searchTerm) || 
                p.Content.ToLower().Contains(searchTerm);
            
            filter = filter == null ? searchFilter : filter.And(searchFilter);
        }
        
        // Category filter
        if (!string.IsNullOrEmpty(request.CategoryId))
        {
            Expression<Func<Post, bool>> categoryFilter = p => 
                p.CategoryId == request.CategoryId;
            
            filter = filter == null ? categoryFilter : filter.And(categoryFilter);
        }
        
        // Rating filter
        if (request.MinRating.HasValue)
        {
            Expression<Func<Post, bool>> ratingFilter = p => 
                p.AverageRating >= request.MinRating.Value;
            
            filter = filter == null ? ratingFilter : filter.And(ratingFilter);
        }
        
        // Date range filter
        if (request.FromDate.HasValue)
        {
            Expression<Func<Post, bool>> fromDateFilter = p => 
                p.CreatedAt >= request.FromDate.Value;
            
            filter = filter == null ? fromDateFilter : filter.And(fromDateFilter);
        }
        
        if (request.ToDate.HasValue)
        {
            Expression<Func<Post, bool>> toDateFilter = p => 
                p.CreatedAt <= request.ToDate.Value;
            
            filter = filter == null ? toDateFilter : filter.And(toDateFilter);
        }
        
        return filter;
    }
    
    /// <summary>
    /// Apply sorting to query options
    /// </summary>
    private void ApplySorting(QueryOptionsBuilder<Post> builder, UnifiedSearchRequest request)
    {
        // Check if using FilterModel sort
        if (request.Sort != null && request.Sort.Any())
        {
            var options = builder.Build();
            FilterModelParser.ApplySorting(options, request.Sort);
            
            if (options.OrderBy != null)
                builder.WithOrderBy(options.OrderBy);
            else if (options.OrderByDescending != null)
                builder.WithOrderByDescending(options.OrderByDescending);
            
            return;
        }
        
        // Use simple sort parameters
        if (!string.IsNullOrEmpty(request.SortBy))
        {
            var sortDescending = request.SortDescending ?? true;
            
            Expression<Func<Post, object>>? sortExpression = request.SortBy.ToLower() switch
            {
                "title" => p => p.Title,
                "rating" => p => p.AverageRating,
                "createdat" => p => p.CreatedAt,
                _ => p => p.CreatedAt
            };
            
            if (sortDescending)
                builder.WithOrderByDescending(sortExpression);
            else
                builder.WithOrderBy(sortExpression);
        }
        else
        {
            // Default sort by CreatedAt descending
            builder.WithOrderByDescending(p => p.CreatedAt);
        }
    }
}
