namespace BlogApi.Application.Common.Services;

public interface ITokenBlacklistService
{
    Task BlacklistTokenAsync(string jti, TimeSpan expiry);
    Task<bool> IsBlacklistedAsync(string jti);
}
