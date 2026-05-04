using FloraCore.Application.Common.Services;
using FloraCore.Application.Features.Auth.DTOs;
using FloraCore.Application.Features.Users.Queries;
using FloraCore.Domain.Entities;
using FloraCore.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace FloraCore.Application.Features.Auth.Commands;

public record RefreshTokenCommand(string AccessToken, string RefreshToken) : IRequest<AuthResponse>;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IJwtService _jwtService;
    private readonly IGenericRepository<RefreshToken, Guid> _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<AppUser> _userManager;

    public RefreshTokenHandler(
        IJwtService jwtService,
        IGenericRepository<RefreshToken, Guid> refreshTokenRepository,
        IUnitOfWork unitOfWork,
        UserManager<AppUser> userManager)
    {
        _jwtService = jwtService;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _userManager = userManager;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var principal = _jwtService.GetPrincipalFromExpiredToken(request.AccessToken);
        var userIdStr = principal!.Claims.First(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub).Value;
        var userId = Guid.Parse(userIdStr);
        
        var refreshTokens = await _refreshTokenRepository.FindAsync(x => x.Token == request.RefreshToken && x.UserId == userId);
        var savedRefreshToken = refreshTokens.FirstOrDefault();

        if (savedRefreshToken == null)
            throw new UnauthorizedAccessException("Invalid refresh token");

        // DETECTION: If token is already used or revoked → possible reuse attack!
        if (savedRefreshToken.IsUsed || savedRefreshToken.IsRevoked)
        {
            // Bulk revoke all tokens for this user — single SQL UPDATE instead of N queries
            await _refreshTokenRepository.GetQueryable()
                .Where(t => t.UserId == userId && !t.IsRevoked)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(t => t.IsRevoked, true),
                    cancellationToken);

            throw new UnauthorizedAccessException("Refresh token reuse detected! All tokens revoked for security.");
        }

        if (savedRefreshToken.IsExpired)
            throw new UnauthorizedAccessException("Refresh token expired");

        var user = await _userManager.FindByIdAsync(userIdStr);
        if (user == null) throw new UnauthorizedAccessException("User not found");

        var roles = await _userManager.GetRolesAsync(user);
        var (newToken, newJti) = _jwtService.GenerateAccessToken(user, roles);
        var newRefreshTokenStr = _jwtService.GenerateRefreshToken();

        // ROTATION: Mark old as used, add new token — commit both atomically
        savedRefreshToken.IsUsed = true;
        savedRefreshToken.ReplacedByToken = newRefreshTokenStr;
        await _refreshTokenRepository.UpdateAsync(savedRefreshToken);

        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = newRefreshTokenStr,
            Jti = newJti,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        await _refreshTokenRepository.AddAsync(newRefreshToken);

        var userDto = new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            UserName = user.UserName ?? string.Empty
        };

        return new AuthResponse(newToken, newRefreshTokenStr, userDto);
    }
}
