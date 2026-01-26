using BlogApi.Application.Features.Favorites.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BlogApi.Controllers;

[ApiController]
[Route("api/[controller]")]
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
