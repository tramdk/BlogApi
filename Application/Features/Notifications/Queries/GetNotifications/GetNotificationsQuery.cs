using BlogApi.Application.Common.Interfaces;
using BlogApi.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlogApi.Application.Features.Notifications.Queries.GetNotifications;

public record GetNotificationsQuery() : IRequest<List<NotificationDto>>;

public record NotificationDto(Guid Id, string Title, string Message, string Type, DateTime CreatedAt, bool IsRead, string? RelatedId);

public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, List<NotificationDto>>
{
    private readonly IGenericRepository<Notification, Guid> _repository;
    private readonly ICurrentUserService _currentUserService;

    public GetNotificationsQueryHandler(IGenericRepository<Notification, Guid> repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<List<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        
        var notifications = await _repository.FindAsync(n => n.UserId == userId);
        
        return notifications
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto(n.Id, n.Title, n.Message, n.Type, n.CreatedAt, n.IsRead, n.RelatedId))
            .ToList();
    }
}
