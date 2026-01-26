using BlogApi.Application.Common.Interfaces;
using BlogApi.Domain.Entities;
using MediatR;
using UUIDNext;

namespace BlogApi.Application.Features.Chat.Commands.SendMessage;

public record SendMessageCommand(Guid ReceiverId, string Message) : IRequest<Guid>;

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, Guid>
{
    private readonly IGenericRepository<ChatMessage, Guid> _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IChatService _chatService;

    public SendMessageCommandHandler(
        IGenericRepository<ChatMessage, Guid> repository, 
        ICurrentUserService currentUserService, 
        IChatService chatService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _chatService = chatService;
    }

    public async Task<Guid> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var senderId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        
        var chatMessage = new ChatMessage
        {
            Id = Uuid.NewDatabaseFriendly(Database.SqlServer),
            SenderId = senderId,
            ReceiverId = request.ReceiverId,
            Message = request.Message,
            SentAt = DateTime.UtcNow
        };

        await _repository.AddAsync(chatMessage);

        // Notify the receiver via real-time service (abstracted from SignalR)
        await _chatService.SendMessageToUserAsync(
            request.ReceiverId, 
            senderId, 
            request.Message, 
            chatMessage.SentAt);

        return chatMessage.Id;
    }
}
