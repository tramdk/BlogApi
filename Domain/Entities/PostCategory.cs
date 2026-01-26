using System;
using System.Collections.Generic;

namespace BlogApi.Domain.Entities;

public class PostCategory
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Post> Posts { get; set; } = new List<Post>();
}
