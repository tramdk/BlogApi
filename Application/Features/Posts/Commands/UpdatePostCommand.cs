using BlogApi.Application.Common.Interfaces;
using BlogApi.Domain.Entities;
using MediatR;

namespace BlogApi.Application.Features.Posts.Commands;

public record UpdatePostCommand(Guid Id, string Title, string Content, string? CategoryId = null) : IRequest<bool>, IOwnershipRequest;

public class UpdatePostCommandHandler : IRequestHandler<UpdatePostCommand, bool>
{
    private readonly IGenericRepository<Post, Guid> _repository;
    public UpdatePostCommandHandler(IGenericRepository<Post, Guid> repository) => _repository = repository;

    public async Task<bool> Handle(UpdatePostCommand request, CancellationToken cancellationToken)
    {
        var post = await _repository.GetByIdAsync(request.Id);
        if (post == null) return false;

        post.Title = request.Title;
        post.Content = request.Content;
        post.CategoryId = request.CategoryId;
        post.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(post);
        return true;
    }
}
