using System;
using System.Threading.Tasks;

namespace BlogApi.Application.Common.Interfaces;

public interface INotificationService
{
    Task SendNotificationToUser(Guid userId, string title, string message, string type, string? relatedId = null);
    Task SendNotificationToAll(string title, string message, string type, string? relatedId = null);
}
