using BlogApi.Application.Common.Interfaces;
using BlogApi.Application.Common.Services;
using BlogApi.Application.Features.Auth.DTOs;
using BlogApi.Application.Features.Users.Queries;
using BlogApi.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UUIDNext;

namespace BlogApi.Application.Features.Auth.Commands;

public record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;

public class LoginHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IJwtService _jwtService;
    private readonly IGenericRepository<RefreshToken, Guid> _refreshTokenRepository;

    public LoginHandler(UserManager<AppUser> userManager, IJwtService jwtService, IGenericRepository<RefreshToken, Guid> refreshTokenRepository)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            throw new UnauthorizedAccessException("Invalid credentials");

        var roles = await _userManager.GetRolesAsync(user);
        var (token, jti) = _jwtService.GenerateAccessToken(user, roles);
        var refreshTokenStr = _jwtService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            Id = Uuid.NewDatabaseFriendly(Database.SqlServer),
            Token = refreshTokenStr,
            Jti = jti,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            UserId = user.Id
        };

        await _refreshTokenRepository.AddAsync(refreshToken);

        var userDto = new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            UserName = user.UserName ?? string.Empty,
            Roles = roles
        };

        return new AuthResponse(token, refreshTokenStr, userDto);
    }
}
