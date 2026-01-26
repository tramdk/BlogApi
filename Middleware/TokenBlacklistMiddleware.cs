using System.IdentityModel.Tokens.Jwt;
using BlogApi.Application.Common.Services;

namespace BlogApi.Middleware;

public class TokenBlacklistMiddleware
{
    private readonly RequestDelegate _next;

    public TokenBlacklistMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ITokenBlacklistService blacklistService)
    {
        var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        
        if (!string.IsNullOrEmpty(token))
        {
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(token))
            {
                var jwtToken = handler.ReadJwtToken(token);
                var jti = jwtToken.Id;

                if (await blacklistService.IsBlacklistedAsync(jti))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Token is blacklisted");
                    return;
                }
            }
        }

        await _next(context);
    }
}
