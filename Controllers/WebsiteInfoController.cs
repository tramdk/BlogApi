using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FloraCore.Application.Features.WebsiteInfo.Commands;
using FloraCore.Application.Features.WebsiteInfo.Queries;
using FloraCore.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace FloraCore.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class WebsiteInfoController : ControllerBase
{
    private readonly IMediator _mediator;

    public WebsiteInfoController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var result = await _mediator.Send(new GetWebsiteInfoQuery(id));
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetAllWebsiteInfoQuery());
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateWebsiteInfoCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateWebsiteInfoCommand command)
    {
        if (id != command.Id) return BadRequest();
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteWebsiteInfoCommand(id));
        return NoContent();
    }
}
