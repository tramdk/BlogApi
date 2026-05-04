using Serilog.Core;
using Serilog.Events;
using System.Collections.Generic;

namespace BlogApi.Infrastructure.Logging;

/// <summary>
/// Serilog enricher to mask sensitive data in log properties.
/// </summary>
public class LogMaskingEnricher : ILogEventEnricher
{
    private static readonly HashSet<string> SensitiveProperties = new()
    {
        "Password", "NewPassword", "OldPassword", "AccessToken", "RefreshToken", "Secret", "ApiKey"
    };

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        foreach (var property in logEvent.Properties)
        {
            if (SensitiveProperties.Contains(property.Key))
            {
                var maskedValue = new ScalarValue("***MASKED***");
                logEvent.AddOrUpdateProperty(new LogEventProperty(property.Key, maskedValue));
            }
        }
    }
}
