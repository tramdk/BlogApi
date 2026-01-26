namespace BlogApi.Application.Features.Posts.DTOs;

public record PostDto(Guid Id, string Title, string AuthorName, double AverageRating, DateTime CreatedAt, string? CategoryId = null);
