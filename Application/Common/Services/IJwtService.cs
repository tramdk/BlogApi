using System.Security.Claims;
using FloraCore.Domain.Entities;

namespace FloraCore.Application.Common.Services;

public interface IJwtService
{
    (string Token, string Jti) GenerateAccessToken(AppUser user, IEnumerable<string> roles);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
