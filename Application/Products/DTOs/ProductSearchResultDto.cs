namespace FloraCore.Application.Products.DTOs;

/// <summary>
/// Represents the result of a product search.
/// </summary>
public record ProductSearchResultDto
{
    /// <summary>
    /// The unique identifier of the product.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The name of the product.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The description of the product.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The price of the product.
    /// </summary>
    public decimal Price { get; init; }

    /// <summary>
    /// The stock quantity of the product.
    /// </summary>
    public int Stock { get; init; }

    /// <summary>
    /// The URL of the product image.
    /// </summary>
    public string? ImageUrl { get; init; }

    /// <summary>
    /// The average customer rating of the product.
    /// </summary>
    public double AverageRating { get; init; }
}