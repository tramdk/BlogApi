using FloraCore.Application.Common.Interfaces;
using FloraCore.Application.Common.Services;
using FloraCore.Application.Common.Models;
using FloraCore.Domain.Entities;
using FloraCore.Infrastructure.Data;
using FloraCore.Infrastructure.Repositories;
using FloraCore.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using OpenTelemetry.Trace;
using Hangfire;
using Hangfire.PostgreSql;
using OpenTelemetry.Resources;
using Microsoft.Extensions.Caching.Hybrid;

namespace FloraCore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        var databaseProvider = configuration["DatabaseProvider"] ?? "SqlServer";
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
        {
            if (databaseProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            {
                options.UseNpgsql(connectionString);
            }
            else
            {
                options.UseSqlServer(connectionString);
            }
            
            options.ConfigureWarnings(warnings => 
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddIdentity<AppUser, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // Repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
        services.AddScoped<IPostQueryService, PostQueryService>();

        // Services
        services.AddSingleton<IDateTimeService, DateTimeService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();
        
        #pragma warning disable EXTEXP0018
        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5)
            };
        });
        #pragma warning restore EXTEXP0018
        
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<OutboxProcessor>();
        services.AddScoped<IFileService, CloudinaryFileService>();
        services.AddScoped<IChatService, ChatService>();

        return services;
    }

    public static IServiceCollection AddSignalRServices(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, FloraCore.Infrastructure.Hubs.UserIdProvider>();
        return services;
    }

    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddConsoleExporter(); // For development. Use OTLP for production.
            });

        return services;
    }

    public static IServiceCollection AddBackgroundTasks(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseProvider = configuration["DatabaseProvider"] ?? "SqlServer";
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddHangfire(config =>
        {
            config.SetDataCompatibilityLevel(Hangfire.CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings();

            if (databaseProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            {
                config.UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString));
            }
            else
            {
                config.UseSqlServerStorage(connectionString);
            }
        });

        services.AddHangfireServer();

        return services;
    }
}
