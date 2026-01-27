using System;
using System.Collections.Generic;
using System.Linq;

namespace BlogApi.Application.Features.Cart.Queries;

public record CartDto
{
    public Guid UserId { get; init; }
    public List<CartItemDto> Items { get; init; } = new();
    public decimal TotalPrice => Items.Sum(i => i.Price * i.Quantity);
}

public record CartItemDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int Quantity { get; init; }
    public string? ImageUrl { get; init; }
}
