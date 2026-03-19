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
    /// 1. GET with query parameters: /api/posts/search?searchTerm=react
    /// 2. POST with simple body: { "searchTerm": "react" }
    /// 3. POST with FilterModel: { "filters": { "title": { "filterType": "text", "type": "contains", "filter": "react" } } }
    /// </summary>
    [HttpGet("search")]
    [HttpGet("unified")] // For backward compatibility
    [AllowAnonymous]
    public async Task<IActionResult> SearchGet(
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? categoryId = null,
        [FromQuery] double? minRating = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool? sortDescending = null,
        [FromQuery] int? page = null,
        [FromQuery] int pageSize = 10)
    {
        var request = new UnifiedSearchRequest
        {
            SearchTerm = searchTerm,
            CategoryId = categoryId,
            MinRating = minRating,
            FromDate = fromDate,
            ToDate = toDate,
            SortBy = sortBy,
            SortDescending = sortDescending,
            Page = page,
            PageSize = pageSize > 0 ? pageSize : 10
        };

        var query = new UnifiedSearchPostsQuery(request);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost("search")]
    [HttpPost("unified")] // For backward compatibility
    [AllowAnonymous]
    public async Task<IActionResult> SearchPost([FromBody] UnifiedSearchRequest request)
    {
        if (request == null)
            return BadRequest();

        var query = new UnifiedSearchPostsQuery(request);
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
