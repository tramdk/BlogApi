using BlogApi.Domain.Entities;
using BlogApi.Application.Common.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace BlogApi.Application.Features.PostCategories.Commands;

public record DeletePostCategoryCommand(string Id) : IRequest<bool>;

public class DeletePostCategoryCommandHandler : IRequestHandler<DeletePostCategoryCommand, bool>
{
    private readonly IGenericRepository<PostCategory, string> _repository;

    public DeletePostCategoryCommandHandler(IGenericRepository<PostCategory, string> repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(DeletePostCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _repository.GetByIdAsync(request.Id);
        if (category == null) return false;

        await _repository.DeleteAsync(category);
        return true;
    }
}
