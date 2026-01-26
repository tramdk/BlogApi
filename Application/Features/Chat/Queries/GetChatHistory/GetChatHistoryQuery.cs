using BlogApi.Application.Common.Interfaces;
using BlogApi.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlogApi.Application.Features.Chat.Queries.GetChatHistory;

public record GetChatHistoryQuery(Guid OtherUserId) : IRequest<List<ChatMessageDto>>;

public record ChatMessageDto(Guid Id, Guid SenderId, Guid ReceiverId, string Message, DateTime SentAt, bool IsRead);

public class GetChatHistoryQueryHandler : IRequestHandler<GetChatHistoryQuery, List<ChatMessageDto>>
{
    private readonly IGenericRepository<ChatMessage, Guid> _repository;
    private readonly ICurrentUserService _currentUserService;

    public GetChatHistoryQueryHandler(IGenericRepository<ChatMessage, Guid> repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<List<ChatMessageDto>> Handle(GetChatHistoryQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();

        var messages = await _repository.FindAsync(m => 
            (m.SenderId == currentUserId && m.ReceiverId == request.OtherUserId) ||
            (m.SenderId == request.OtherUserId && m.ReceiverId == currentUserId));

        return messages
            .OrderBy(m => m.SentAt)
            .Select(m => new ChatMessageDto(m.Id, m.SenderId, m.ReceiverId, m.Message, m.SentAt, m.IsRead))
            .ToList();
    }
}
