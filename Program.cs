using System.IdentityModel.Tokens.Jwt;
using FloraCore.Application.Common.Extensions;
using FloraCore.Extensions;
using FloraCore.Infrastructure;
using FloraCore.Infrastructure.Data;
using Hangfire;
using Serilog;
using Asp.Versioning;

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
builder.Services.AddAppHealthChecks(builder.Configuration);
builder.Services.AddAppResponseCompression();
builder.Services.AddAppResilience();

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

// ========== Configure Middleware Pipeline & Endpoints ==========
app.UseAppMiddlewares();
app.MapAppEndpoints();

// ========== Database Initialization & Seeding ==========
if (!app.Environment.IsEnvironment("Testing"))
{
    await DatabaseSeederExtensions.InitializeDatabaseAsync(app);
}

if (app.Environment.IsDevelopment() || Environment.GetEnvironmentVariable("SEED_DATA") == "true")
{
    await DatabaseSeederExtensions.SeedDatabaseAsync(app);
}

await DatabaseSeederExtensions.SyncTokenBlacklistAsync(app);

// ========== Hangfire Recurring Jobs ==========
var jobManager = app.Services.GetRequiredService<IRecurringJobManager>();
jobManager.AddOrUpdate<FloraCore.Infrastructure.Services.OutboxProcessor>(
    "outbox-processor",
    processor => processor.ProcessMessagesAsync(),
    Cron.Minutely());

app.Run();

// Required for integration tests
public partial class Program { }
