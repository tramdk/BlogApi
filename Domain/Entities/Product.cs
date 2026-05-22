using System;
using System.Collections.Generic;

namespace FloraCore.Domain.Entities;

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
		/// <summary>
    /// The promotion rate of the product.
    /// </summary>
    public decimal PromotionRate { get; set; } = 0;
    public int Stock { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    public Guid? CategoryId { get; set; }
    public ProductCategory? Category { get; set; }
    
    public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
    public double AverageRating { get; set; }

		/// <summary>
    /// Gets the discounted price of the product.
    /// </summary>
    /// <returns>The discounted price.</returns>
    public decimal GetDiscountedPrice()
    {
        return Price * (1 - PromotionRate / 100);
    }
}
