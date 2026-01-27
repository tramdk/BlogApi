using System;
using System.Collections.Generic;

namespace BlogApi.Application.Features.Products.Queries;

public record ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public string? ImageUrl { get; init; }
    public double AverageRating { get; init; }
    public Guid? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public List<ReviewDto> Reviews { get; init; } = new();
}

public record ReviewDto
{
    public Guid Id { get; init; }
    public string UserName { get; init; } = string.Empty;
    public int Rating { get; init; }
    public string Comment { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
