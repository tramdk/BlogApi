using Microsoft.AspNetCore.Identity;

namespace BlogApi.Domain.Entities;

public class AppUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public Cart? Cart { get; set; }
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();
}
