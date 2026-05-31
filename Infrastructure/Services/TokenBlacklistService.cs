using FloraCore.Application.Common.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FloraCore.Infrastructure.Services;

/// <summary>
/// Service for blacklisting JWT tokens.
/// </summary>
public class TokenBlacklistService(IDistributedCache cache, ILogger<TokenBlacklistService> logger) : ITokenBlacklistService
{
    private readonly IDistributedCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private readonly ILogger<TokenBlacklistService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Blacklists a token by adding it to the cache with an expiration time.
    /// </summary>
    /// <param name="jti">The JWT ID (JTI) of the token.</param>
    /// <param name="expiry">The expiration time.</param>
    public async Task BlacklistTokenAsync(string jti, TimeSpan expiry)
    {
        try
        {
            await _cache.SetStringAsync($"blacklist_{jti}", "true", new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blacklisting token with JTI: {Jti}", jti);
        }
    }

    /// <summary>
    /// Checks if a token is blacklisted.
    /// </summary>
    /// <param name="jti">The JWT ID (JTI) of the token.</param>
    /// <returns>True if the token is blacklisted, otherwise false.</returns>
    public async Task<bool> IsBlacklistedAsync(string jti)
    {
        try
        {
            return await _cache.GetStringAsync($"blacklist_{jti}") != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if token is blacklisted. JTI: {Jti}", jti);
            return false;
        }
    }
}
