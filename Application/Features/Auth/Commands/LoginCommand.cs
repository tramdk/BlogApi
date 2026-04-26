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


namespace BlogApi.Application.Features.Auth.Commands;

public record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;

/// <summary>
/// Handler for user login requests.
/// </summary>
/// <param name="userManager">The manager for user identity operations.</param>
/// <param name="jwtService">The service for generating JWT tokens.</param>
/// <param name="refreshTokenRepository">The repository for storing refresh tokens.</param>
public class LoginHandler(
    UserManager<AppUser> userManager, 
    IJwtService jwtService, 
    IGenericRepository<RefreshToken, Guid> refreshTokenRepository) : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly UserManager<AppUser> _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    private readonly IJwtService _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
    private readonly IGenericRepository<RefreshToken, Guid> _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));

    /// <summary>
    /// Handles the login request by validating credentials and generating tokens.
    /// </summary>
    /// <param name="request">The login command containing credentials.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An AuthResponse containing access and refresh tokens along with user details.</returns>
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
            Id = Guid.NewGuid(),
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
