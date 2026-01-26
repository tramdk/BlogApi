using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace BlogApi.Infrastructure.Hubs;

public class UserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        // Use 'sub' claim since we cleared the default mapping in Program.cs
        return connection.User?.FindFirst("sub")?.Value;
    }
}
