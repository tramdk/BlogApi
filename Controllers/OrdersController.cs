
using FloraCore.Application.Features.Orders.Commands;
using FloraCore.Application.Features.Orders.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using FloraCore.Application.Common.Constants; // ThÃªm dÃ²ng nÃ y
using Asp.Versioning;

namespace FloraCore.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class OrdersController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var result = await _mediator.Send(new GetOrderByIdQuery(id));
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = RoleConstants.Admin)]
    public async Task<IActionResult> GetOrders([FromQuery] GetOrdersQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Gets the current authenticated user's orders with pagination and status filtering.
    /// </summary>
    /// <param name="query">The pagination and filter options.</param>
    /// <returns>A paged list of the user's orders.</returns>
    [HttpGet("my")]
    public async Task<IActionResult> GetMyOrders([FromQuery] GetMyOrdersQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(Get), new { id = id }, id);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateOrderCommand command)
    {
        if (id != command.Id) return BadRequest();
        var result = await _mediator.Send(command);
        return result ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteOrderCommand(id));
        return result ? NoContent() : NotFound();
    }

    [HttpPost("{orderId}/items")]
    public async Task<IActionResult> AddOrderItem(Guid orderId, AddOrderItemCommand command)
    {
        if (orderId != command.OrderId) return BadRequest();
        var result = await _mediator.Send(command);
        return result ? Ok() : BadRequest();
    }

    [HttpDelete("{orderId}/items/{orderItemId}")]
    public async Task<IActionResult> RemoveOrderItem(Guid orderId, Guid orderItemId)
    {
        var result = await _mediator.Send(new RemoveOrderItemCommand(orderId, orderItemId));
        return result ? NoContent() : NotFound();
    }

    /// <summary>
    /// Gets order statistics for administrators.
    /// </summary>
    /// <param name="startDate">Optional start date to filter orders.</param>
    /// <param name="endDate">Optional end date to filter orders.</param>
    /// <returns>Order statistics.</returns>
    [HttpGet("statistics")]
    [Authorize(Roles = RoleConstants.Admin)] // Only Admin can access
    public async Task<IActionResult> GetOrderStatistics(
        [FromQuery] DateTime? startDate, 
        [FromQuery] DateTime? endDate)
    {
        var query = new GetOrderStatisticsQuery { StartDate = startDate, EndDate = endDate };
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
