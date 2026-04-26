using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BlogApi.Application.Common.Attributes;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace BlogApi.Application.Common.Behaviors;

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> 
    where TRequest : IRequest<TResponse>
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(IDistributedCache cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var cacheAttribute = (CacheableAttribute?)Attribute.GetCustomAttribute(typeof(TRequest), typeof(CacheableAttribute));
        if (cacheAttribute == null) return await next();

        var cacheKey = BuildCacheKey(request);
        var cachedResponse = await _cache.GetStringAsync(cacheKey, cancellationToken);

        if (cachedResponse != null)
        {
            _logger.LogInformation("Cache hit for {RequestType}", typeof(TRequest).Name);
            return JsonSerializer.Deserialize<TResponse>(cachedResponse)!;
        }

        var response = await next();
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheAttribute.ExpirationMinutes)
        };

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(response), options, cancellationToken);
        _logger.LogInformation("Cache miss for {RequestType}, stored with key hash", typeof(TRequest).Name);

        return response;
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
