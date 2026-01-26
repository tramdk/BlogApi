using BlogApi.Application.Common.Services;
using BlogApi.Domain.Entities;
using BlogApi.Application.Common.Interfaces;
using MediatR;
using System.IdentityModel.Tokens.Jwt;

namespace BlogApi.Application.Features.Auth.Commands;

public record LogoutCommand(string AccessToken) : IRequest<bool>;

public class LogoutHandler : IRequestHandler<LogoutCommand, bool>
{
    private readonly ITokenBlacklistService _blacklistService;
    private readonly IGenericRepository<RefreshToken, int> _refreshTokenRepository;

    public LogoutHandler(ITokenBlacklistService blacklistService, IGenericRepository<RefreshToken, int> refreshTokenRepository)
    {
        _blacklistService = blacklistService;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<bool> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(request.AccessToken);
        var jti = jwtToken.Id;
        var expiry = jwtToken.ValidTo - DateTime.UtcNow;

        if (expiry > TimeSpan.Zero)
        {
            await _blacklistService.BlacklistTokenAsync(jti, expiry);
        }

        var tokens = await _refreshTokenRepository.FindAsync(x => x.Jti == jti);
        var rt = tokens.FirstOrDefault();
        if (rt != null)
        {
            rt.IsRevoked = true;
            await _refreshTokenRepository.UpdateAsync(rt);
        }

        return true;
    }
}
