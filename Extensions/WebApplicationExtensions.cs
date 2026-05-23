using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Hangfire;
using Scalar.AspNetCore;
using FloraCore.Middleware;
using FloraCore.Application.Common.Constants;
using AspNetCoreRateLimit;

namespace FloraCore.Extensions;

/// <summary>
/// Extension methods for WebApplication to organize middleware pipeline and endpoint routing.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Configure the HTTP request pipeline with middlewares.
    /// </summary>
    public static WebApplication UseAppMiddlewares(this WebApplication app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseRouting();
        app.UseCors(CorsConstants.AllowFrontend);

        app.UseSecurityHeaders(); // Use SecurityHeaders middleware with configuration registered in DI
        app.UseResponseCompression();
        app.UseSerilogRequestLogging();

        // Custom Health Checks Endpoint
        app.UseHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var response = new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(entry => new
                    {
                        name = entry.Key,
                        status = entry.Value.Status.ToString(),
                        description = entry.Value.Description,
                        duration = entry.Value.Duration
                    }),
                    totalDuration = report.TotalDuration
                };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }));
            }
        });

        // Scalar API Documentation
        app.UseSwagger(options => options.RouteTemplate = "openapi/{documentName}.json");
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("Blog API v1")
                   .WithTheme(ScalarTheme.Moon)
                   .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        });

        if (!app.Environment.IsEnvironment("Testing"))
        {
            app.UseIpRateLimiting();
        }

        app.UseAuthentication();
        app.UseMiddleware<TokenBlacklistMiddleware>();
        app.UseAuthorization();

        return app;
    }

    /// <summary>
    /// Map Web API endpoints, SignalR hubs, and Hangfire dashboard.
    /// </summary>
    public static WebApplication MapAppEndpoints(this WebApplication app)
    {
        app.MapControllers();

        // Hangfire Dashboard (Restricted to authenticated Admin users)
        if (!app.Environment.IsEnvironment("Testing"))
        {
            app.MapHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = [new FloraCore.Infrastructure.Security.HangfireDashboardAuthFilter()]
            });

            // Register Hangfire Recurring Jobs
            var jobManager = app.Services.GetRequiredService<IRecurringJobManager>();
            jobManager.AddOrUpdate<FloraCore.Infrastructure.Services.OutboxProcessor>(
                "outbox-processor",
                processor => processor.ProcessMessagesAsync(),
                Cron.Minutely());
        }

        // SignalR Hubs
        app.MapHub<FloraCore.Infrastructure.Hubs.ChatHub>("/hubs/chat");
        app.MapHub<FloraCore.Infrastructure.Hubs.NotificationHub>("/hubs/notifications");

        return app;
    }
}
