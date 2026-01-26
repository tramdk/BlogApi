namespace BlogApi.Application.Common.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class CacheableAttribute : Attribute
{
    public int ExpirationMinutes { get; set; } = 10;
}
