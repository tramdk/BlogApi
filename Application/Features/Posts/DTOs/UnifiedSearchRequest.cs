using BlogApi.Application.Common.Models;

namespace BlogApi.Application.Features.Posts.DTOs;

/// <summary>
/// Unified search request that supports multiple approaches:
/// 1. Simple parameters (searchTerm, categoryId, etc.)
/// 2. FilterModel (AG-Grid, MUI DataGrid style)
/// 3. Mixed approach
/// </summary>
public class UnifiedSearchRequest
{
    // ========== Simple Search Parameters ==========
    
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
    public string? SortBy { get; set; }
    
    /// <summary>
    /// Sort direction: true for descending, false for ascending
    /// </summary>
    public bool? SortDescending { get; set; }
    
    // ========== Advanced FilterModel ==========
    
    /// <summary>
    /// Advanced filter model (AG-Grid, MUI DataGrid style)
    /// If provided, this takes precedence over simple parameters
    /// </summary>
    public Dictionary<string, FilterCondition>? Filters { get; set; }
    
    /// <summary>
    /// Sort model (AG-Grid, MUI DataGrid style)
    /// If provided, this takes precedence over SortBy/SortDescending
    /// </summary>
    public List<SortModel>? Sort { get; set; }
    
    // ========== Pagination ==========
    
    /// <summary>
    /// Page number (0-based for FilterModel, 1-based for simple search)
    /// </summary>
    public int? Page { get; set; }
    
    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; } = 10;
    
    // ========== Helper Methods ==========
    
    /// <summary>
    /// Check if this request uses FilterModel approach
    /// </summary>
    public bool IsFilterModelRequest => Filters != null && Filters.Any();
    
    /// <summary>
    /// Check if this request uses simple search approach
    /// </summary>
    public bool IsSimpleSearchRequest => !IsFilterModelRequest && 
        (!string.IsNullOrEmpty(SearchTerm) || 
         !string.IsNullOrEmpty(CategoryId) || 
         MinRating.HasValue || 
         FromDate.HasValue || 
         ToDate.HasValue);
    
    /// <summary>
    /// Get effective page number (convert to 0-based)
    /// </summary>
    public int GetEffectivePage()
    {
        if (Page == null) return 0;
        
        // If using FilterModel, page is already 0-based
        if (IsFilterModelRequest) return Page.Value;
        
        // If using simple search, page is 1-based, convert to 0-based
        return Math.Max(0, Page.Value - 1);
    }
}
