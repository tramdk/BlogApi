using System.Text;
using AspNetCoreRateLimit;
using BlogApi.Application.Common.Behaviors;
using BlogApi.Application.Common.Interfaces;
using BlogApi.Application.Common.Services;
using BlogApi.Domain.Entities;
using BlogApi.Infrastructure.Data;
using BlogApi.Infrastructure.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using BlogApi.Application.Common.Constants;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace BlogApi.Application.Common.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to organize DI registration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add Database Context and Identity services. Supports SqlServer and PostgreSQL.
    /// </summary>
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseProvider = configuration["DatabaseProvider"] ?? "SqlServer";
        var connectionString = configuration.GetConnectionString(ConfigurationKeys.DefaultConnection);

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
        });

        services.AddIdentity<AppUser, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }

    /// <summary>
    /// Add JWT Authentication services.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration[ConfigurationKeys.JwtIssuer],
                ValidAudience = configuration[ConfigurationKeys.JwtAudience],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration[ConfigurationKeys.JwtSecret]!))
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }

    /// <summary>
    /// Add Rate Limiting services.
    /// </summary>
    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.Configure<IpRateLimitOptions>(configuration.GetSection(ConfigurationKeys.IpRateLimiting));
        services.AddInMemoryRateLimiting();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        return services;
    }

    /// <summary>
    /// Add Application layer services (MediatR, Validators, CurrentUserService).
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, params Type[] assemblyMarkerTypes)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddValidatorsFromAssemblyContaining<Program>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<Program>();
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
        });

        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());


        return services;
    }

    /// <summary>
    /// Add Infrastructure layer services (repositories, external services).
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Unit of Work & Repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
        services.AddScoped<IPostQueryService, PostQueryService>();

        // Core Services
        services.AddSingleton<IDateTimeService, BlogApi.Infrastructure.Services.DateTimeService>();
        services.AddScoped<IJwtService, BlogApi.Infrastructure.Services.JwtService>();
        services.AddScoped<ITokenBlacklistService, BlogApi.Infrastructure.Services.TokenBlacklistService>();
        
        // External Services
        services.AddScoped<INotificationService, BlogApi.Infrastructure.Services.NotificationService>();
        services.AddScoped<IFileService, BlogApi.Infrastructure.Services.FileService>();
        services.AddScoped<IChatService, BlogApi.Infrastructure.Services.ChatService>();

        return services;
    }

    /// <summary>
    /// Add SignalR services with User ID Provider.
    /// </summary>
    public static IServiceCollection AddSignalRServices(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, BlogApi.Infrastructure.Hubs.UserIdProvider>();
        return services;
    }

    /// <summary>
    /// Add CORS policy for frontend applications.
    /// </summary>
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, string policyName = CorsConstants.AllowFrontend)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(policyName, policy =>
            {
                policy.SetIsOriginAllowed(_ => true) // Allow any origin during development
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// Add OpenAPI Generator (Swashbuckle) configuration for Scalar UI.
    /// </summary>
    public static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo 
            { 
                Title = "Blog API", 
                Version = "v1",
                Description = "A Clean Architecture API for Blogging Platform",
                Contact = new OpenApiContact { Name = "Developer", Email = "dev@example.com" }
            });

            // JWT Configuration
            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter JWT Bearer token **_only_**",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer", 
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Id = JwtBearerDefaults.AuthenticationScheme,
                    Type = ReferenceType.SecurityScheme
                }
            };

            options.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { securityScheme, Array.Empty<string>() }
            });

            // XML Documentation
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }
}
