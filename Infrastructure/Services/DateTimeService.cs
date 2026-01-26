using BlogApi.Application.Common.Interfaces;

namespace BlogApi.Infrastructure.Services;

/// <summary>
/// Implementation of IDateTimeService for production use.
/// </summary>
public class DateTimeService : IDateTimeService
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime Now => DateTime.Now;
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}
