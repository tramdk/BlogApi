using BlogApi.Application.Features.PostCategories.Commands;
using BlogApi.Application.Features.PostCategories.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlogApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostCategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public PostCategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<PostCategoryDto>>> GetAll()
    {
        return await _mediator.Send(new GetPostCategoriesQuery());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PostCategoryDto>> GetById(string id)
    {
        var category = await _mediator.Send(new GetPostCategoryByIdQuery(id));
        if (category == null) return NotFound();
        return category;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<string>> Create(CreatePostCategoryCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(string id, UpdatePostCategoryCommand command)
    {
        if (id != command.Id) return BadRequest();
        var result = await _mediator.Send(command);
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _mediator.Send(new DeletePostCategoryCommand(id));
        if (!result) return NotFound();
        return NoContent();
    }
}
