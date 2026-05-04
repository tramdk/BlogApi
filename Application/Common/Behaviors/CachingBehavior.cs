using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BlogApi.Application.Common.Attributes;
using Microsoft.Extensions.Caching.Hybrid;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BlogApi.Application.Common.Behaviors;

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> 
    where TRequest : IRequest<TResponse>
{
    private readonly HybridCache _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(HybridCache cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var cacheAttribute = (CacheableAttribute?)Attribute.GetCustomAttribute(typeof(TRequest), typeof(CacheableAttribute));
        if (cacheAttribute == null) return await next();

        var cacheKey = BuildCacheKey(request);
        
        #pragma warning disable EXTEXP0018
        return await _cache.GetOrCreateAsync(
            cacheKey, 
            async ct => await next(), 
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(cacheAttribute.ExpirationMinutes) },
            cancellationToken: cancellationToken);
        #pragma warning restore EXTEXP0018
    }

    /// <summary>
    /// Builds a stable, bounded cache key using SHA256 hash of the serialized request.
    /// Prevents overly long keys and potential collisions from raw JSON.
    /// </summary>
    private static string BuildCacheKey(TRequest request)
    {
        var serialized = JsonSerializer.Serialize(request);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(serialized));
        var hashHex = Convert.ToHexString(hash)[..16]; // Use first 16 hex chars (64-bit hash)
        return $"Cache_{typeof(TRequest).Name}_{hashHex}";
    }
}
