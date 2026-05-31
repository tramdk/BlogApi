using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FloraCore.Application.Common.Services;
using FloraCore.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using FloraCore.Application.Common.Models;
using System;
using System.Collections.Generic;
using FloraCore.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using FloraCore.Application.Common.Interfaces;

namespace FloraCore.Infrastructure.Services;

/// <summary>
/// Service for generating and validating JWT tokens.
/// </summary>
public class JwtService(
    IOptions<JwtSettings> jwtSettings, 
    ILogger<JwtService> logger,
    IResourceManager resourceManager) : IJwtService
{
    private readonly IOptions<JwtSettings> _jwtSettings = jwtSettings ?? throw new ArgumentNullException(nameof(jwtSettings));
    private readonly ILogger<JwtService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IResourceManager _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));

    /// <summary>
    /// Generates an access token for the given user and roles.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="roles">The roles.</param>
    /// <returns>The generated access token and JTI.</returns>
    public (string Token, string Jti) GenerateAccessToken(AppUser user, IEnumerable<string> roles)
    {
        var jti = Guid.NewGuid().ToString();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, jti)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Value.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Value.Issuer,
            audience: _jwtSettings.Value.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.Value.ExpiryMinutes),
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), jti);
    }

    /// <summary>
    /// Generates a refresh token.
    /// </summary>
    /// <returns>The generated refresh token.</returns>
    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    /// <summary>
    /// Gets the principal from an expired token.
    /// </summary>
    /// <param name="token">The token.</param>
    /// <returns>The principal.</returns>
    /// <exception cref="SecurityTokenException">Thrown if the token is invalid.</exception>
    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Value.Secret)),
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        SecurityToken securityToken;
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new InvalidJwtTokenException(_resourceManager.GetString("InvalidTokenAlgorithm"));

            return principal;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogError(ex, "Invalid JWT token.");
            throw new InvalidJwtTokenException(_resourceManager.GetString("InvalidJwtToken"), ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token.");
            throw new InvalidJwtTokenException(_resourceManager.GetString("ErrorValidatingToken"), ex);
        }
    }
}
