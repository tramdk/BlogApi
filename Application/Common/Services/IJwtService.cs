using System.Security.Claims;
using BlogApi.Domain.Entities;

namespace BlogApi.Application.Common.Services;

public interface IJwtService
{
    (string Token, string Jti) GenerateAccessToken(AppUser user, IEnumerable<string> roles);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
