using BlogApi.Application.Features.Posts.Queries;
using BlogApi.Application.Features.Posts.Commands;
using BlogApi.Application.Features.Posts.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PostsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get all posts with basic pagination
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? cursor = null, [FromQuery] int pageSize = 10)
    {
        var result = await _mediator.Send(new GetPostsQuery(cursor, pageSize));
        return Ok(result);
    }

    /// <summary>
    /// Unified search endpoint - supports multiple approaches:
    /// 1. GET with query parameters: /api/posts/unified?searchTerm=react&minRating=4.0
    /// 2. POST with simple body: { "searchTerm": "react", "minRating": 4.0 }
    /// 3. POST with FilterModel: { "filters": { "title": { "filterType": "text", "type": "contains", "filter": "react" } } }
    /// </summary>
    [HttpGet("unified")]
    [HttpPost("unified")]
    [AllowAnonymous]
    public async Task<IActionResult> UnifiedSearch(
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? categoryId = null,
        [FromQuery] double? minRating = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool? sortDescending = null,
        [FromQuery] int? page = null,
        [FromQuery] int pageSize = 10,
        [FromBody] UnifiedSearchRequest? bodyRequest = null)
    {
        // Merge query parameters and body request
        var request = bodyRequest ?? new UnifiedSearchRequest();
        
        // Query parameters take precedence if body is empty
        if (bodyRequest == null || (!bodyRequest.IsFilterModelRequest && !bodyRequest.IsSimpleSearchRequest))
        {
            request.SearchTerm = searchTerm ?? request.SearchTerm;
            request.CategoryId = categoryId ?? request.CategoryId;
            request.MinRating = minRating ?? request.MinRating;
            request.FromDate = fromDate ?? request.FromDate;
            request.ToDate = toDate ?? request.ToDate;
            request.SortBy = sortBy ?? request.SortBy;
            request.SortDescending = sortDescending ?? request.SortDescending;
            request.Page = page ?? request.Page;
            request.PageSize = pageSize > 0 ? pageSize : request.PageSize;
        }
        
        var query = new UnifiedSearchPostsQuery(request);
        var result = await _mediator.Send(query);
        return Ok(result);
    }


    /// <summary>
    /// Search posts with advanced filtering, sorting, and pagination
    /// </summary>
    /// <param name="searchTerm">Search in title and content</param>
    /// <param name="categoryId">Filter by category</param>
    /// <param name="minRating">Minimum rating (0-5)</param>
    /// <param name="fromDate">Filter posts from this date</param>
    /// <param name="toDate">Filter posts to this date</param>
    /// <param name="sortBy">Sort field: Title, Rating, CreatedAt (default)</param>
    /// <param name="sortDescending">Sort direction: true (default) or false</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <returns>Paged result with posts</returns>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search(
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? categoryId = null,
        [FromQuery] double? minRating = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string sortBy = "CreatedAt",
        [FromQuery] bool sortDescending = true,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new SearchPostsQuery(
            searchTerm,
            categoryId,
            minRating,
            fromDate,
            toDate,
            sortBy,
            sortDescending,
            pageNumber,
            pageSize
        );

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Search posts with POST request (for complex filters in body)
    /// </summary>
    /// <param name="request">Search request with filters</param>
    /// <returns>Paged result with posts</returns>
    [HttpPost("search")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchPost([FromBody] SearchPostsRequest request)
    {
        var query = new SearchPostsQuery(
            request.SearchTerm,
            request.CategoryId,
            request.MinRating,
            request.FromDate,
            request.ToDate,
            request.SortBy,
            request.SortDescending,
            request.PageNumber,
            request.PageSize
        );

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Search posts with FilterModel (AG-Grid, MUI DataGrid, DevExtreme style)
    /// </summary>
    /// <param name="filterModel">Filter model with dynamic filters and sorting</param>
    /// <returns>Paged result with posts</returns>
    [HttpPost("filter")]
    [AllowAnonymous]
    public async Task<IActionResult> FilterPosts([FromBody] BlogApi.Application.Common.Models.FilterModel filterModel)
    {
        var query = new SearchPostsWithFilterModelQuery(filterModel);
        var result = await _mediator.Send(query);
        return Ok(result);
    }


    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPost(Guid id)
    {
        var query = new GetPostDetailQuery(id);
        var response = await _mediator.Send(query);
        if (response == null) return NotFound();
        return Ok(response);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreatePostCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpPost("{id}/rate")]
    [Authorize]
    public async Task<IActionResult> Rate(Guid id, [FromBody] int score)
    {
        var result = await _mediator.Send(new RatePostCommand(id, score));
        return result ? Ok() : NotFound();
    }
    
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePostCommand command)
    {
        if (id != command.Id) return BadRequest("ID mismatch");
        var result = await _mediator.Send(command);
        return result ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeletePostCommand(id));
        return result ? NoContent() : NotFound();
    }
}
