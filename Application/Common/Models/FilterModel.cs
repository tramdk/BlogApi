namespace BlogApi.Application.Common.Models;

/// <summary>
/// Generic filter model for dynamic filtering (AG-Grid, MUI DataGrid style)
/// </summary>
public class FilterModel
{
    /// <summary>
    /// Dictionary of column filters: { "columnName": FilterCondition }
    /// </summary>
    public Dictionary<string, FilterCondition> Filters { get; set; } = new();
    
    /// <summary>
    /// Sort model
    /// </summary>
    public List<SortModel> Sort { get; set; } = new();
    
    /// <summary>
    /// Page number (0-based or 1-based depending on client)
    /// </summary>
    public int Page { get; set; } = 0;
    
    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// Filter condition for a single column
/// </summary>
public class FilterCondition
{
    /// <summary>
    /// Filter type: text, number, date, boolean, set
    /// </summary>
    public string FilterType { get; set; } = "text";
    
    /// <summary>
    /// Operator: equals, notEqual, contains, startsWith, endsWith, 
    /// lessThan, lessThanOrEqual, greaterThan, greaterThanOrEqual,
    /// inRange, blank, notBlank
    /// </summary>
    public string Type { get; set; } = "contains";
    
    /// <summary>
    /// Filter value (for single value filters)
    /// </summary>
    public object? Filter { get; set; }
    
    /// <summary>
    /// Filter values (for set filters)
    /// </summary>
    public List<object>? Values { get; set; }
    
    /// <summary>
    /// Date from (for date range filters)
    /// </summary>
    public DateTime? DateFrom { get; set; }
    
    /// <summary>
    /// Date to (for date range filters)
    /// </summary>
    public DateTime? DateTo { get; set; }
    
    /// <summary>
    /// Condition 1 (for AND/OR conditions)
    /// </summary>
    public FilterCondition? Condition1 { get; set; }
    
    /// <summary>
    /// Condition 2 (for AND/OR conditions)
    /// </summary>
    public FilterCondition? Condition2 { get; set; }
    
    /// <summary>
    /// Operator between conditions: AND, OR
    /// </summary>
    public string? Operator { get; set; }
}

/// <summary>
/// Sort model for a column
/// </summary>
public class SortModel
{
    /// <summary>
    /// Column ID
    /// </summary>
    public string ColId { get; set; } = string.Empty;
    
    /// <summary>
    /// Sort direction: asc, desc
    /// </summary>
    public string Sort { get; set; } = "asc";
}
