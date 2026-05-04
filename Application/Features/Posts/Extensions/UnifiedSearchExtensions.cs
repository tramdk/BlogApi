using FloraCore.Application.Features.Posts.DTOs;

namespace FloraCore.Application.Features.Posts.Extensions;

public static class UnifiedSearchExtensions
{
    /// <summary>
    /// Check if this request uses FilterModel approach
    /// </summary>
    public static bool IsFilterModelRequest(this UnifiedSearchRequest request) 
        => request.Filters != null && request.Filters.Any();

    /// <summary>
    /// Check if this request uses simple search approach
    /// </summary>
    public static bool IsSimpleSearchRequest(this UnifiedSearchRequest request) 
        => !request.IsFilterModelRequest() && 
           (!string.IsNullOrEmpty(request.SearchTerm) || 
            !string.IsNullOrEmpty(request.CategoryId) || 
            request.MinRating.HasValue || 
            request.FromDate.HasValue || 
            request.ToDate.HasValue);

    /// <summary>
    /// Get effective page number (convert to 0-based)
    /// </summary>
    public static int GetEffectivePage(this UnifiedSearchRequest request)
    {
        if (request.Page == null) return 0;
        
        // If using FilterModel, page is already 0-based
        if (request.IsFilterModelRequest()) return request.Page.Value;
        
        // If using simple search, page is 1-based, convert to 0-based
        return Math.Max(0, request.Page.Value - 1);
    }
}
