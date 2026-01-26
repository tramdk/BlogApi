using BlogApi.Application.Common.Interfaces;
using BlogApi.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BlogApi.Infrastructure.Services;

/// <summary>
/// Implementation of IChatService using SignalR.
/// This service handles real-time chat message delivery.
/// </summary>
public class ChatService : IChatService
{
    private readonly IHubContext<ChatHub, IChatClient> _hubContext;

    public ChatService(IHubContext<ChatHub, IChatClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendMessageToUserAsync(Guid receiverId, Guid senderId, string message, DateTime sentAt)
    {
        await _hubContext.Clients
            .User(receiverId.ToString())
            .ReceiveMessage(senderId, message, sentAt);
    }
}
