using System;
using System.Collections.Generic;

namespace BlogApi.Domain.Entities;

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    public Guid? CategoryId { get; set; }
    public ProductCategory? Category { get; set; }
    
    public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
    public double AverageRating { get; set; }
}
