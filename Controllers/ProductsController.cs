using BlogApi.Application.Common.Models;
using BlogApi.Application.Features.Products.Commands;
using BlogApi.Application.Features.Products.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Asp.Versioning;

namespace BlogApi.Controllers;

/// <summary>
/// Controller for managing products.
/// </summary>
/// <param name="mediator">The mediator instance for handling commands and queries.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProductsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    /// <summary>
    /// Gets a paged list of products.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="search">Optional search term.</param>
    /// <returns>A paged result of products.</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        return Ok(await _mediator.Send(new GetProductsQuery(page, pageSize, search)));
    }

    /// <summary>
    /// Gets a product by its ID.
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <returns>The product details or NotFound.</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id)
    {
        var product = await _mediator.Send(new GetProductByIdQuery(id));
        if (product == null) return NotFound();
        return Ok(product);
    }

    /// <summary>
    /// Creates a new product.
    /// </summary>
    /// <param name="command">The command containing product details.</param>
    /// <returns>The ID of the newly created product.</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Guid>> Create(CreateProductCommand command)
    {
        return Ok(await _mediator.Send(command));
    }

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="command">The command containing updated product details.</param>
    /// <returns>NoContent if successful, or BadRequest if ID mismatch.</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Update(Guid id, UpdateProductCommand command)
    {
        if (id != command.Id) return BadRequest();
        await _mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Deletes a product.
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <returns>NoContent if successful.</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteProductCommand(id));
        return NoContent();
    }
}
