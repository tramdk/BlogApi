namespace BlogApi.Application.Common.Interfaces;

/// <summary>
/// Service interface for real-time chat functionality.
/// This abstraction allows the Application layer to send chat messages
/// without depending on SignalR infrastructure details.
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Sends a chat message to a specific user via real-time connection.
    /// </summary>
    Task SendMessageToUserAsync(Guid receiverId, Guid senderId, string message, DateTime sentAt);
}
