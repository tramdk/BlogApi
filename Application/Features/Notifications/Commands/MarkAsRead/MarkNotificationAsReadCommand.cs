using BlogApi.Application.Common.Interfaces;
using BlogApi.Domain.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlogApi.Application.Features.Notifications.Commands.MarkAsRead;

public record MarkNotificationAsReadCommand(Guid Id) : IRequest<bool>;

public class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand, bool>
{
    private readonly IGenericRepository<Notification, Guid> _repository;
    private readonly ICurrentUserService _currentUserService;

    public MarkNotificationAsReadCommandHandler(IGenericRepository<Notification, Guid> repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var notification = await _repository.GetByIdAsync(request.Id);

        if (notification == null || notification.UserId != userId) return false;

        notification.IsRead = true;
        await _repository.UpdateAsync(notification);
        
        return true;
    }
}
