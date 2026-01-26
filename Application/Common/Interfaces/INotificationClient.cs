using System.Threading.Tasks;

namespace BlogApi.Application.Common.Interfaces;

public interface INotificationClient
{
    Task ReceiveNotification(string title, string message, string type, string? relatedId);
}
