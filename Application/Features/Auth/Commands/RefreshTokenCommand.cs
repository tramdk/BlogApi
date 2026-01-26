using BlogApi.Application.Common.Services;
using BlogApi.Application.Features.Auth.DTOs;
using BlogApi.Application.Features.Users.Queries;
using BlogApi.Domain.Entities;
using BlogApi.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UUIDNext;

namespace BlogApi.Application.Features.Auth.Commands;

public record RefreshTokenCommand(string AccessToken, string RefreshToken) : IRequest<AuthResponse>;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IJwtService _jwtService;
    private readonly IGenericRepository<RefreshToken, Guid> _refreshTokenRepository;
    private readonly UserManager<AppUser> _userManager;

    public RefreshTokenHandler(IJwtService jwtService, IGenericRepository<RefreshToken, Guid> refreshTokenRepository, UserManager<AppUser> userManager)
    {
        _jwtService = jwtService;
        _refreshTokenRepository = refreshTokenRepository;
        _userManager = userManager;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var principal = _jwtService.GetPrincipalFromExpiredToken(request.AccessToken);
        var userIdStr = principal!.Claims.First(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub).Value;
        var userId = Guid.Parse(userIdStr);
        
        var refreshTokens = await _refreshTokenRepository.FindAsync(x => x.Token == request.RefreshToken && x.UserId == userId);
        var savedRefreshToken = refreshTokens.FirstOrDefault();

        if (savedRefreshToken == null || savedRefreshToken.IsRevoked || savedRefreshToken.ExpiryDate <= DateTime.UtcNow)
            throw new UnauthorizedAccessException("Invalid refresh token");

        var user = await _userManager.FindByIdAsync(userIdStr);
        if (user == null) throw new UnauthorizedAccessException("User not found");

        var roles = await _userManager.GetRolesAsync(user);
        var (newToken, newJti) = _jwtService.GenerateAccessToken(user, roles);
        var newRefreshTokenStr = _jwtService.GenerateRefreshToken();

        savedRefreshToken.IsRevoked = true; // Rotation: Revoke old one
        await _refreshTokenRepository.UpdateAsync(savedRefreshToken);
        
        await _refreshTokenRepository.AddAsync(new RefreshToken
        {
            Id = Uuid.NewDatabaseFriendly(Database.SqlServer),
            Token = newRefreshTokenStr,
            Jti = newJti,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            UserId = userId
        });

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
