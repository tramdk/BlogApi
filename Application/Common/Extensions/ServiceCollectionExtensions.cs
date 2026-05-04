using System.Text;
using AspNetCoreRateLimit;
using AspNetCoreRateLimit.Redis;
using BlogApi.Application.Common.Behaviors;
using BlogApi.Application.Common.Interfaces;
using BlogApi.Application.Common.Services;
using BlogApi.Domain.Entities;
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
    /// Add Redis Cache services.
    /// </summary>
    public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString(ConfigurationKeys.Redis);
        var skipRedis = Environment.GetEnvironmentVariable("SKIP_REDIS") == "true";

        if (string.IsNullOrEmpty(redisConnectionString) || skipRedis)
        {
            services.AddDistributedMemoryCache();
        }
        else
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "BlogApi_";
            });
        }
        return services;
    }

    /// <summary>
    /// Add Rate Limiting services.
    /// </summary>
    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.Configure<IpRateLimitOptions>(configuration.GetSection(ConfigurationKeys.IpRateLimiting));

        var redisConnectionString = configuration.GetConnectionString(ConfigurationKeys.Redis);
        var skipRedis = Environment.GetEnvironmentVariable("SKIP_REDIS") == "true";

        if (!string.IsNullOrEmpty(redisConnectionString) && !skipRedis)
        {
            // Use Distributed Cache (Redis) for Rate Limiting if available
            services.AddDistributedRateLimiting();
        }
        else
        {
            services.AddInMemoryRateLimiting();
        }

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

        services.AddAutoMapper(cfg => {}, typeof(Program).Assembly);


        return services;
    }



    /// <summary>
    /// Add CORS policy for frontend applications.
    /// </summary>
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration, string policyName = CorsConstants.AllowFrontend)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(policyName, policy =>
            {
                var angularUrl = configuration["AngularApp:Url"];
                var reactUrl = configuration["ReactApp:Url"];
                var allowedOrigins = new List<string>();
                
                if (!string.IsNullOrEmpty(angularUrl)) allowedOrigins.Add(angularUrl.TrimEnd('/'));
                if (!string.IsNullOrEmpty(reactUrl)) allowedOrigins.Add(reactUrl.TrimEnd('/'));

                if (allowedOrigins.Any())
                {
                    policy.WithOrigins(allowedOrigins.ToArray())
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                }
                else
                {
                    // Fallback for local development when origins are not configured
                    policy.WithOrigins(
                              "http://localhost:3000",
                              "http://localhost:4200",
                              "http://localhost:5173",
                              "http://localhost:8080")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                }
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
