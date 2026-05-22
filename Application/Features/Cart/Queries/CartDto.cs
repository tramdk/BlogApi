using System;
using System.Collections.Generic;
using System.Linq;

namespace FloraCore.Application.Features.Cart.Queries;

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
		/// <summary>
    /// The original price of the product.
    /// </summary>
		public decimal OriginalPrice { get; init; }
    /// <summary>
    /// The price of the product.
    /// </summary>
		public decimal Price { get; init; }
		/// <summary>
    /// The promotion rate of the product.
    /// </summary>
		public decimal PromotionRate { get; init; }
    public int Quantity { get; init; }
    public string? ImageUrl { get; init; }
}
