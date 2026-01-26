namespace BlogApi.Application.Features.Posts.DTOs;

/// <summary>
/// Request DTO for searching posts with filters and sorting
/// </summary>
public class SearchPostsRequest
{
    /// <summary>
    /// Search term to match in title or content
    /// </summary>
    public string? SearchTerm { get; set; }
    
    /// <summary>
    /// Filter by category ID
    /// </summary>
    public string? CategoryId { get; set; }
    
    /// <summary>
    /// Minimum rating (0-5)
    /// </summary>
    public double? MinRating { get; set; }
    
    /// <summary>
    /// Filter posts created from this date
    /// </summary>
    public DateTime? FromDate { get; set; }
    
    /// <summary>
    /// Filter posts created to this date
    /// </summary>
    public DateTime? ToDate { get; set; }
    
    /// <summary>
    /// Sort field: Title, Rating, CreatedAt
    /// </summary>
    public string SortBy { get; set; } = "CreatedAt";
    
    /// <summary>
    /// Sort direction: true for descending, false for ascending
    /// </summary>
    public bool SortDescending { get; set; } = true;
    
    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int PageNumber { get; set; } = 1;
    
    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; } = 10;
}
