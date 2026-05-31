using System.Collections.Generic;

namespace FloraCore.Application.Features.Orders.DTOs;

/// <summary>
/// DTO for order statistics.
/// </summary>
public record OrderStatisticsDto
{
    /// <summary>
    /// Total number of orders.
    /// </summary>
    public int TotalOrders { get; init; }

    /// <summary>
    /// Total revenue from all orders.
    /// </summary>
    public decimal TotalRevenue { get; init; }

    /// <summary>
    /// Average value of an order.
    /// </summary>
    public decimal AverageOrderValue { get; init; }

    /// <summary>
    /// Dictionary of order counts by status (e.g., "Pending": 10, "Delivered": 50).
    /// </summary>
    public Dictionary<string, int> OrdersByStatus { get; init; } = new();

    /// <summary>
    /// Dictionary of revenue by month (e.g., "2023-01": 1500.00, "2023-02": 2000.00).
    /// Key format: YYYY-MM.
    /// </summary>
    public Dictionary<string, decimal> RevenueByMonth { get; init; } = new();
}
