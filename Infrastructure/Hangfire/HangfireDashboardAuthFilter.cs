using Hangfire.Dashboard;

namespace FloraCore.Infrastructure.Security;

/// <summary>
/// Authorization filter for the Hangfire Dashboard.
/// Only allows authenticated users with the "Admin" role.
/// </summary>
public class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Must be authenticated
        if (httpContext.User.Identity?.IsAuthenticated != true)
            return false;

        // Must have Admin role
        return httpContext.User.IsInRole("Admin");
    }
}
