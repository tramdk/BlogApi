using FloraCore.Application.Common.Interfaces;
using FloraCore.Application.Common.Services;
using FloraCore.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FloraCore.Application.Features.Auth.Commands;

public record ChangePasswordCommand(string OldPassword, string NewPassword) : IRequest<bool>;

public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, bool>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ICurrentUserService _currentUserService;
    private readonly IGenericRepository<RefreshToken, Guid> _refreshTokenRepository;
    private readonly ITokenBlacklistService _blacklistService;

    public ChangePasswordHandler(
        UserManager<AppUser> userManager,
        ICurrentUserService currentUserService,
        IGenericRepository<RefreshToken, Guid> refreshTokenRepository,
        ITokenBlacklistService blacklistService)
    {
        _userManager = userManager;
        _currentUserService = currentUserService;
        _refreshTokenRepository = refreshTokenRepository;
        _blacklistService = blacklistService;
    }

    public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            throw new UnauthorizedAccessException();

        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (user == null)
            return false;

        var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

        // Invalidate all existing tokens for the user
        var userTokens = await _refreshTokenRepository.FindAsync(rt => rt.UserId == userId.Value && !rt.IsRevoked);
        foreach (var rt in userTokens)
        {
            rt.IsRevoked = true;
            await _refreshTokenRepository.UpdateAsync(rt);

            var expiry = rt.ExpiryDate - DateTime.UtcNow;
            if (expiry > TimeSpan.Zero)
            {
                await _blacklistService.BlacklistTokenAsync(rt.Jti, expiry);
            }
        }

        return true;
    }
}
