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

        string cacheKey = $"Cache_{typeof(TRequest).Name}_{JsonSerializer.Serialize(request)}";
        var cachedResponse = await _cache.GetStringAsync(cacheKey, cancellationToken);

        if (cachedResponse != null)
        {
            _logger.LogInformation("Cache hit for {Key}", cacheKey);
            return JsonSerializer.Deserialize<TResponse>(cachedResponse)!;
        }

        var response = await next();
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheAttribute.ExpirationMinutes)
        };

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(response), options, cancellationToken);
        _logger.LogInformation("Cache missed for {Key}, added to cache", cacheKey);

        return response;
    }
}
