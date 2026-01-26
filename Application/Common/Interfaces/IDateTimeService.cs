namespace BlogApi.Application.Common.Interfaces;

/// <summary>
/// Service interface for getting current date/time.
/// Allows for testable date/time operations.
/// </summary>
public interface IDateTimeService
{
    /// <summary>
    /// Gets the current UTC date and time.
    /// </summary>
    DateTime UtcNow { get; }

    /// <summary>
    /// Gets the current date and time in local timezone.
    /// </summary>
    DateTime Now { get; }

    /// <summary>
    /// Gets the current date (without time component) in UTC.
    /// </summary>
    DateOnly Today { get; }
}
