using System.Text;
using AspNetCoreRateLimit;
using AspNetCoreRateLimit.Redis;
using FloraCore.Application.Common.Behaviors;
using FloraCore.Application.Common.Interfaces;
using FloraCore.Application.Common.Services;
using FloraCore.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using FloraCore.Application.Common.Constants;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace FloraCore.Application.Common.Extensions;

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
                options.InstanceName = "FloraCore_";
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
                var allowedOrigins = new List<string>();

                // 1. Check for a comma-separated list in "AllowedOrigins" (Best for Prod)
                var originsConfig = configuration["AllowedOrigins"];
                if (!string.IsNullOrEmpty(originsConfig))
                {
                    allowedOrigins.AddRange(originsConfig.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim().TrimEnd('/')));
                }

                // 2. Backward compatibility with existing config keys
                var angularUrl = configuration["AngularApp:Url"];
                var reactUrl = configuration["ReactApp:Url"];
                
                if (!string.IsNullOrEmpty(angularUrl)) allowedOrigins.Add(angularUrl.Trim().TrimEnd('/'));
                if (!string.IsNullOrEmpty(reactUrl)) allowedOrigins.Add(reactUrl.Trim().TrimEnd('/'));

                // 3. Add the specific Vercel origin reported by the user
                allowedOrigins.Add("https://tiemhoachinchin.vercel.app");

                if (allowedOrigins.Any())
                {
                    policy.WithOrigins(allowedOrigins.Distinct().ToArray())
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                }
                else
                {
                    // Fallback for local development
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
