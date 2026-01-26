using BlogApi.Application.Common.Interfaces;
using BlogApi.Domain.Entities;
using MediatR;

namespace BlogApi.Application.Features.Posts.Commands;

public record DeletePostCommand(Guid Id) : IRequest<bool>, IOwnershipRequest;

public class DeletePostCommandHandler : IRequestHandler<DeletePostCommand, bool>
{
    private readonly IGenericRepository<Post, Guid> _repository;
    public DeletePostCommandHandler(IGenericRepository<Post, Guid> repository) => _repository = repository;

    public async Task<bool> Handle(DeletePostCommand request, CancellationToken cancellationToken)
    {
        var post = await _repository.GetByIdAsync(request.Id);
        if (post == null) return false;

        await _repository.DeleteAsync(post);
        return true;
    }
}
