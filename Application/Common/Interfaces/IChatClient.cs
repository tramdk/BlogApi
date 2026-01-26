using System;
using System.Threading.Tasks;

namespace BlogApi.Application.Common.Interfaces;

public interface IChatClient
{
    Task ReceiveMessage(Guid senderId, string message, DateTime sentAt);
    Task MessageSent(Guid messageId);
}
