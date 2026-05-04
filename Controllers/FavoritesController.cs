using BlogApi.Application.Features.Favorites.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

using Asp.Versioning;

namespace BlogApi.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly IMediator _mediator;

    public FavoritesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("toggle/{productId}")]
    public async Task<ActionResult<bool>> ToggleFavorite(Guid productId)
    {
        return Ok(await _mediator.Send(new ToggleFavoriteCommand(productId)));
    }
}
