using System;
using BlogApi.Application.Common.Interfaces;
using BlogApi.Domain.Entities;
using BlogApi.Infrastructure.Hubs;
using BlogApi.Infrastructure.Repositories;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using UUIDNext;

namespace BlogApi.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IGenericRepository<Notification, Guid> _repository;
    private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;

    public NotificationService(IGenericRepository<Notification, Guid> repository, IHubContext<NotificationHub, INotificationClient> hubContext)
    {
        _repository = repository;
        _hubContext = hubContext;
    }

    public async Task SendNotificationToUser(Guid userId, string title, string message, string type, string? relatedId = null)
    {
        var notification = new Notification
        {
            Id = Uuid.NewDatabaseFriendly(Database.SqlServer),
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            CreatedAt = DateTime.UtcNow,
            IsRead = false,
            RelatedId = relatedId
        };

        await _repository.AddAsync(notification);

        await _hubContext.Clients.User(userId.ToString()).ReceiveNotification(title, message, type, relatedId);
    }

    public async Task SendNotificationToAll(string title, string message, string type, string? relatedId = null)
    {
        await _hubContext.Clients.All.ReceiveNotification(title, message, type, relatedId);
    }
}
