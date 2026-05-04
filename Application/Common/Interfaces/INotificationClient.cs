using System.Threading.Tasks;

namespace FloraCore.Application.Common.Interfaces;

public interface INotificationClient
{
    Task ReceiveNotification(string title, string message, string type, string? relatedId);
}
