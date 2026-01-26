using BlogApi.Application.Common.Services;
using Microsoft.Extensions.Caching.Distributed;

namespace BlogApi.Infrastructure.Services;

public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly IDistributedCache _cache;
    public TokenBlacklistService(IDistributedCache cache) => _cache = cache;

    public async Task BlacklistTokenAsync(string jti, TimeSpan expiry)
    {
        await _cache.SetStringAsync($"blacklist_{jti}", "true", new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry
        });
    }

    public async Task<bool> IsBlacklistedAsync(string jti)
    {
        return await _cache.GetStringAsync($"blacklist_{jti}") != null;
    }
}
