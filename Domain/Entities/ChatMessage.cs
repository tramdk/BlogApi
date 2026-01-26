using System;

namespace BlogApi.Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public AppUser Sender { get; set; } = null!;
    public Guid ReceiverId { get; set; }
    public AppUser Receiver { get; set; } = null!;
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
}
