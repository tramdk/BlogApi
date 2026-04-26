using System.Security.Claims;
using BlogApi.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace BlogApi.Application.Common.Services;

/// <summary>
/// Retrieves the current authenticated user's ID from the HTTP context claims.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return userId != null ? Guid.Parse(userId) : null;
        }
    }
}
