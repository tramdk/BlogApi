using FloraCore.Application.Interfaces;
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
using OpenTelemetry.Metrics;
using Hangfire;
using Hangfire.PostgreSql;
using OpenTelemetry.Resources;
using Microsoft.Extensions.Caching.Hybrid;

namespace FloraCore.Infrastructure;

/// <summary>
/// Lớp mở rộng chứa các phương thức đăng ký dịch vụ của tầng Infrastructure vào DI Container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký các dịch vụ cốt lõi của tầng Infrastructure bao gồm Database, Identity, Repositories và Services.
    /// </summary>
    /// <param name="services">DI container của ứng dụng.</param>
    /// <param name="configuration">Cấu hình hệ thống.</param>
    /// <returns>Trả về <see cref="IServiceCollection"/> để tiếp tục chuỗi đăng ký.</returns>
    /// <exception cref="ArgumentNullException">Ném ra nếu services hoặc configuration bị null.</exception>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

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
        services.AddScoped<IProductRepository, ProductRepository>();
        
        // Register IPostQueryDialect strategy dynamically based on DatabaseProvider
        if (databaseProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IPostQueryDialect, PostgresPostQueryDialect>();
        }
        else if (databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IPostQueryDialect, SqlServerPostQueryDialect>();
        }
        else
        {
            services.AddSingleton<IPostQueryDialect, SqlitePostQueryDialect>();
        }

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

    /// <summary>
    /// Đăng ký các dịch vụ liên quan đến SignalR phục vụ truyền thông thời gian thực.
    /// </summary>
    /// <param name="services">DI container của ứng dụng.</param>
    /// <returns>Trả về <see cref="IServiceCollection"/> để tiếp tục chuỗi đăng ký.</returns>
    /// <exception cref="ArgumentNullException">Ném ra nếu services bị null.</exception>
    public static IServiceCollection AddSignalRServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSignalR();
        services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, FloraCore.Infrastructure.Hubs.UserIdProvider>();
        return services;
    }

    /// <summary>
    /// Đăng ký và cấu hình hệ thống giám sát OpenTelemetry sử dụng mô hình Strongly-typed Configuration.
    /// </summary>
    /// <param name="services">DI container của ứng dụng.</param>
    /// <param name="configuration">Cấu hình hệ thống.</param>
    /// <returns>Trả về <see cref="IServiceCollection"/> để tiếp tục chuỗi đăng ký.</returns>
    /// <exception cref="ArgumentNullException">Ném ra nếu services hoặc configuration bị null.</exception>
    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Bắt buộc cấu hình Strongly-typed và thực hiện xác thực Data Annotations khi khởi động
        services.AddOptions<TelemetryOptions>()
            .Bind(configuration.GetSection(TelemetryOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Đọc giá trị cấu hình trực tiếp để thiết lập OpenTelemetry
        var telemetryOptions = new TelemetryOptions();
        configuration.GetSection(TelemetryOptions.SectionName).Bind(telemetryOptions);

        var otlpEndpoint = telemetryOptions.OtlpEndpoint;

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("FloraCore"))
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation();

                if (telemetryOptions.ExportToConsole)
                {
                    tracing.AddConsoleExporter();
                }

                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                    });
                }
            })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (telemetryOptions.ExportToConsole)
                {
                    metrics.AddConsoleExporter();
                }

                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                    });
                }
            });

        return services;
    }

    /// <summary>
    /// Đăng ký và cấu hình Hangfire phục vụ các tác vụ chạy ngầm định kỳ.
    /// </summary>
    /// <param name="services">DI container của ứng dụng.</param>
    /// <param name="configuration">Cấu hình hệ thống.</param>
    /// <returns>Trả về <see cref="IServiceCollection"/> để tiếp tục chuỗi đăng ký.</returns>
    /// <exception cref="ArgumentNullException">Ném ra nếu services hoặc configuration bị null.</exception>
    public static IServiceCollection AddBackgroundTasks(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

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
