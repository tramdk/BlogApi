using BlogApi.Application.Features.Cart.Commands;
using BlogApi.Application.Features.Cart.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BlogApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly IMediator _mediator;

    public CartController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart()
    {
        return Ok(await _mediator.Send(new GetCartQuery()));
    }

    [HttpPost("add")]
    public async Task<ActionResult> AddToCart(AddToCartCommand command)
    {
        await _mediator.Send(command);
        return Ok();
    }

    // [HttpPut("update-quantity")]
    // public async Task<ActionResult> UpdateQuantity(UpdateCartItemQuantityCommand command)
    // {
    //     await _mediator.Send(command);
    //     return Ok();
    // }

    [HttpPut("update")]
    public async Task<ActionResult> UpdateQuantity(UpdateCartItemQuantityCommand command)
    {
        await _mediator.Send(command);
        return Ok();
    }

    [HttpDelete("remove/{productId}")]
    public async Task<ActionResult> RemoveFromCart(Guid productId)
    {
        await _mediator.Send(new RemoveFromCartCommand(productId));
        return NoContent();
    }
}
