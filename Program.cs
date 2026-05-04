using FloraCore.Application.Common.Extensions;
using FloraCore.Infrastructure.Data;
using FloraCore.Infrastructure;
using FloraCore.Middleware;
using Serilog;
using Hangfire;
using System.IdentityModel.Tokens.Jwt;
using AspNetCoreRateLimit;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Asp.Versioning;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Clear default claim type mapping for JWT
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// ========== Configure Logging ==========
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.With<FloraCore.Infrastructure.Logging.LogMaskingEnricher>()
    .CreateLogger();
builder.Host.UseSerilog();

// ========== Configure Services ==========
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSignalRServices();
builder.Services.AddObservability(builder.Configuration);
builder.Services.AddBackgroundTasks(builder.Configuration);
builder.Services.AddRedisCache(builder.Configuration)
    .AddRateLimiting(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddCorsPolicy(builder.Configuration);

builder.Services.AddControllers(options =>
    {
        options.Filters.Add<FloraCore.Filters.ApiResponseFilter>();
    })
    .AddJsonOptions(options => 
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddOpenApiDocumentation();
builder.Services.AddHealthChecks();

// Add Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
    options.MimeTypes = Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/json", "text/plain", "image/svg+xml"]);
});

builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Optimal;
});

// Add Polly Resilience Pipeline
builder.Services.AddResiliencePipeline("external-services", pipelineBuilder =>
{
    pipelineBuilder.AddRetry(new Polly.Retry.RetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(2),
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true
    })
    .AddCircuitBreaker(new Polly.CircuitBreaker.CircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5,
        SamplingDuration = TimeSpan.FromSeconds(30),
        MinimumThroughput = 5,
        BreakDuration = TimeSpan.FromSeconds(15)
    });
});

// Add API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

var app = builder.Build();

// ========== Configure Middleware Pipeline ==========
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseResponseCompression();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();
app.UseHealthChecks("/health");

// Enable Docs in all environments for Demo purpose
app.UseSwagger(options => options.RouteTemplate = "openapi/{documentName}.json");
app.MapScalarApiReference(options =>
{
    options.WithTitle("Blog API v1")
           .WithTheme(ScalarTheme.Moon)
           .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.UseRouting();
app.UseCors(FloraCore.Application.Common.Constants.CorsConstants.AllowFrontend);

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseIpRateLimiting();
}

app.UseAuthentication();
app.UseMiddleware<TokenBlacklistMiddleware>();
app.UseAuthorization();

// ========== Map Endpoints ==========
app.MapControllers();
app.MapHangfireDashboard("/hangfire", new DashboardOptions
{
    // Restrict to authenticated Admin users only
    Authorization = [new FloraCore.Infrastructure.Security.HangfireDashboardAuthFilter()]
});
app.MapHub<FloraCore.Infrastructure.Hubs.ChatHub>("/hubs/chat");
app.MapHub<FloraCore.Infrastructure.Hubs.NotificationHub>("/hubs/notifications");

// ========== Database Seeding ==========
// ========== Database Initialization & Seeding ==========
// 1. Create Tables (Always run to ensure DB exists, except in Integration Tests)
if (!app.Environment.IsEnvironment("Testing"))
{
    await DatabaseSeederExtensions.InitializeDatabaseAsync(app);
}

// 2. Seed Data (Optional in Prod to save RAM)
if (app.Environment.IsDevelopment() || Environment.GetEnvironmentVariable("SEED_DATA") == "true")
{
    await DatabaseSeederExtensions.SeedDatabaseAsync(app);
}

// 3. Sync Token Blacklist to Cache (Crucial for security when using in-memory cache and app restarts)
await DatabaseSeederExtensions.SyncTokenBlacklistAsync(app);

// ========== Hangfire Recurring Jobs ==========
// NOTE: Hangfire manages its own DI scope per job execution — do NOT capture a scope here.
var jobManager = app.Services.GetRequiredService<IRecurringJobManager>();
jobManager.AddOrUpdate<FloraCore.Infrastructure.Services.OutboxProcessor>(
    "outbox-processor",
    processor => processor.ProcessMessagesAsync(),
    Cron.Minutely());

app.Run();

// Required for integration tests
public partial class Program { }
