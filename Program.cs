using System.IdentityModel.Tokens.Jwt;
using AspNetCoreRateLimit;
using BlogApi.Application.Common.Extensions;
using BlogApi.Infrastructure.Data;
using BlogApi.Middleware;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Clear default claim type mapping for JWT
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// ========== Configure Logging ==========
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();
builder.Host.UseSerilog();

// ========== Configure Services ==========
builder.Services
    .AddDatabaseServices(builder.Configuration)
    .AddDistributedMemoryCache()
    .AddRateLimiting(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddSignalRServices()
    .AddApplicationServices()
    .AddInfrastructureServices()
    .AddCorsPolicy();

builder.Services.AddControllers();
builder.Services.AddOpenApiDocumentation();
builder.Services.AddHealthChecks();

var app = builder.Build();

// ========== Configure Middleware Pipeline ==========
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
app.UseCors(BlogApi.Application.Common.Constants.CorsConstants.AllowFrontend);

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseIpRateLimiting();
}

app.UseAuthentication();
app.UseMiddleware<TokenBlacklistMiddleware>();
app.UseAuthorization();

// ========== Map Endpoints ==========
app.MapControllers();
app.MapHub<BlogApi.Infrastructure.Hubs.ChatHub>("/hubs/chat");
app.MapHub<BlogApi.Infrastructure.Hubs.NotificationHub>("/hubs/notifications");

// ========== Database Seeding ==========
// ========== Database Initialization & Seeding ==========
// 1. Create Tables (Always run to ensure DB exists)
await app.InitializeDatabaseAsync();

// 2. Seed Data (Optional in Prod to save RAM)
if (app.Environment.IsDevelopment() || Environment.GetEnvironmentVariable("SEED_DATA") == "true")
{
    await app.SeedDatabaseAsync();
}

app.Run();

// Required for integration tests
public partial class Program { }
