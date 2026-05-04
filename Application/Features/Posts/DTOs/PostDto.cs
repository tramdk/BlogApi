namespace FloraCore.Application.Features.Posts.DTOs;

public record PostDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public double AverageRating { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CategoryId { get; init; }
}
