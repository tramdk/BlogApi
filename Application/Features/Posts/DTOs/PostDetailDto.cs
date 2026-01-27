namespace BlogApi.Application.Features.Posts.DTOs;

public record PostDetailDto(Guid Id, string Title, string Content, double AverageRating, int TotalRatings, string? CategoryId = null, string? AuthorName = null, DateTime CreatedAt = default, DateTime? UpdatedAt = null);
