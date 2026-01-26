using BlogApi.Application.Common.Extensions;
using BlogApi.Application.Common.Models;
using System.Linq.Expressions;
using System.Reflection;

namespace BlogApi.Application.Common.Helpers;

/// <summary>
/// Parser to convert FilterModel to LINQ Expression predicates
/// </summary>
public static class FilterModelParser
{
    /// <summary>
    /// Parse FilterModel to Expression predicate
    /// </summary>
    public static Expression<Func<T, bool>>? ParseFilter<T>(FilterModel filterModel) where T : class
    {
        if (filterModel.Filters == null || !filterModel.Filters.Any())
            return null;
        
        Expression<Func<T, bool>>? combinedFilter = null;
        
        foreach (var filter in filterModel.Filters)
        {
            var columnName = filter.Key;
            var condition = filter.Value;
            
            var predicate = BuildPredicate<T>(columnName, condition);
            
            if (predicate != null)
            {
                combinedFilter = combinedFilter == null 
                    ? predicate 
                    : combinedFilter.And(predicate);
            }
        }
        
        return combinedFilter;
    }
    
    /// <summary>
    /// Build predicate for a single column filter
    /// </summary>
    private static Expression<Func<T, bool>>? BuildPredicate<T>(string columnName, FilterCondition condition)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var property = GetProperty<T>(columnName);
        
        if (property == null)
            return null;
        
        var propertyAccess = Expression.Property(parameter, property);
        
        Expression? filterExpression = condition.FilterType.ToLower() switch
        {
            "text" => BuildTextFilter(propertyAccess, condition),
            "number" => BuildNumberFilter(propertyAccess, condition),
            "date" => BuildDateFilter(propertyAccess, condition),
            "boolean" => BuildBooleanFilter(propertyAccess, condition),
            "set" => BuildSetFilter(propertyAccess, condition),
            _ => null
        };
        
        if (filterExpression == null)
            return null;
        
        return Expression.Lambda<Func<T, bool>>(filterExpression, parameter);
    }
    
    /// <summary>
    /// Build text filter expression
    /// </summary>
    private static Expression? BuildTextFilter(MemberExpression property, FilterCondition condition)
    {
        if (condition.Filter == null)
            return null;
        
        var filterValue = condition.Filter.ToString()!;
        var filterValueExpr = Expression.Constant(filterValue, typeof(string));
        
        // Handle null check
        var nullCheck = Expression.NotEqual(property, Expression.Constant(null, property.Type));
        
        Expression comparison = condition.Type.ToLower() switch
        {
            "equals" => Expression.Equal(property, filterValueExpr),
            "notequal" => Expression.NotEqual(property, filterValueExpr),
            "contains" => Expression.Call(property, "Contains", null, filterValueExpr),
            "notcontains" => Expression.Not(Expression.Call(property, "Contains", null, filterValueExpr)),
            "startswith" => Expression.Call(property, "StartsWith", null, filterValueExpr),
            "endswith" => Expression.Call(property, "EndsWith", null, filterValueExpr),
            "blank" => Expression.Or(
                Expression.Equal(property, Expression.Constant(null, property.Type)),
                Expression.Equal(property, Expression.Constant(string.Empty))),
            "notblank" => Expression.And(
                Expression.NotEqual(property, Expression.Constant(null, property.Type)),
                Expression.NotEqual(property, Expression.Constant(string.Empty))),
            _ => Expression.Call(property, "Contains", null, filterValueExpr)
        };
        
        // Combine with null check for non-null operations
        if (condition.Type.ToLower() != "blank")
        {
            comparison = Expression.AndAlso(nullCheck, comparison);
        }
        
        return comparison;
    }
    
    /// <summary>
    /// Build number filter expression
    /// </summary>
    private static Expression? BuildNumberFilter(MemberExpression property, FilterCondition condition)
    {
        if (condition.Filter == null)
            return null;
        
        var filterValue = Convert.ToDouble(condition.Filter);
        var propertyType = property.Type;
        
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        var convertedValue = Convert.ChangeType(filterValue, underlyingType);
        var filterValueExpr = Expression.Constant(convertedValue, propertyType);
        
        return condition.Type.ToLower() switch
        {
            "equals" => Expression.Equal(property, filterValueExpr),
            "notequal" => Expression.NotEqual(property, filterValueExpr),
            "lessthan" => Expression.LessThan(property, filterValueExpr),
            "lessthanorequal" => Expression.LessThanOrEqual(property, filterValueExpr),
            "greaterthan" => Expression.GreaterThan(property, filterValueExpr),
            "greaterthanorequal" => Expression.GreaterThanOrEqual(property, filterValueExpr),
            "inrange" when condition.DateFrom.HasValue && condition.DateTo.HasValue => 
                Expression.AndAlso(
                    Expression.GreaterThanOrEqual(property, Expression.Constant(Convert.ChangeType(condition.DateFrom.Value, underlyingType), propertyType)),
                    Expression.LessThanOrEqual(property, Expression.Constant(Convert.ChangeType(condition.DateTo.Value, underlyingType), propertyType))),
            _ => Expression.Equal(property, filterValueExpr)
        };
    }
    
    /// <summary>
    /// Build date filter expression
    /// </summary>
    private static Expression? BuildDateFilter(MemberExpression property, FilterCondition condition)
    {
        return condition.Type.ToLower() switch
        {
            "equals" when condition.Filter != null => 
                Expression.Equal(property, Expression.Constant(Convert.ToDateTime(condition.Filter), property.Type)),
            
            "greaterthan" when condition.Filter != null => 
                Expression.GreaterThan(property, Expression.Constant(Convert.ToDateTime(condition.Filter), property.Type)),
            
            "lessthan" when condition.Filter != null => 
                Expression.LessThan(property, Expression.Constant(Convert.ToDateTime(condition.Filter), property.Type)),
            
            "inrange" when condition.DateFrom.HasValue && condition.DateTo.HasValue => 
                Expression.AndAlso(
                    Expression.GreaterThanOrEqual(property, Expression.Constant(condition.DateFrom.Value, property.Type)),
                    Expression.LessThanOrEqual(property, Expression.Constant(condition.DateTo.Value, property.Type))),
            
            _ => null
        };
    }
    
    /// <summary>
    /// Build boolean filter expression
    /// </summary>
    private static Expression? BuildBooleanFilter(MemberExpression property, FilterCondition condition)
    {
        if (condition.Filter == null)
            return null;
        
        var filterValue = Convert.ToBoolean(condition.Filter);
        return Expression.Equal(property, Expression.Constant(filterValue, property.Type));
    }
    
    /// <summary>
    /// Build set filter expression (IN clause)
    /// </summary>
    private static Expression? BuildSetFilter(MemberExpression property, FilterCondition condition)
    {
        if (condition.Values == null || !condition.Values.Any())
            return null;
        
        var propertyType = property.Type;
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        
        // Convert values to property type
        var convertedValues = condition.Values
            .Select(v => Convert.ChangeType(v, underlyingType))
            .ToList();
        
        // Create list constant
        var listType = typeof(List<>).MakeGenericType(underlyingType);
        var list = Activator.CreateInstance(listType);
        var addMethod = listType.GetMethod("Add")!;
        
        foreach (var value in convertedValues)
        {
            addMethod.Invoke(list, new[] { value });
        }
        
        var listExpr = Expression.Constant(list);
        
        // Call Contains method
        var containsMethod = listType.GetMethod("Contains", new[] { underlyingType })!;
        return Expression.Call(listExpr, containsMethod, property);
    }
    
    /// <summary>
    /// Get property by name (case-insensitive)
    /// </summary>
    private static PropertyInfo? GetProperty<T>(string propertyName)
    {
        return typeof(T).GetProperty(
            propertyName, 
            BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
    }
    
    /// <summary>
    /// Parse sort model to QueryOptions
    /// </summary>
    public static void ApplySorting<T>(QueryOptions<T> options, List<SortModel> sortModel) where T : class
    {
        if (sortModel == null || !sortModel.Any())
            return;
        
        var firstSort = sortModel.First();
        var property = GetProperty<T>(firstSort.ColId);
        
        if (property == null)
            return;
        
        var parameter = Expression.Parameter(typeof(T), "x");
        var propertyAccess = Expression.Property(parameter, property);
        var conversion = Expression.Convert(propertyAccess, typeof(object));
        var lambda = Expression.Lambda<Func<T, object>>(conversion, parameter);
        
        if (firstSort.Sort.ToLower() == "desc")
        {
            options.OrderByDescending = lambda;
        }
        else
        {
            options.OrderBy = lambda;
        }
    }
}
