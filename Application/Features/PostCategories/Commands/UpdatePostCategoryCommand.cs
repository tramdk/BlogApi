using BlogApi.Domain.Entities;
using BlogApi.Application.Common.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlogApi.Application.Features.PostCategories.Commands;

public record UpdatePostCategoryCommand(string Id, string Name) : IRequest<bool>;

public class UpdatePostCategoryCommandHandler : IRequestHandler<UpdatePostCategoryCommand, bool>
{
    private readonly IGenericRepository<PostCategory, string> _repository;

    public UpdatePostCategoryCommandHandler(IGenericRepository<PostCategory, string> repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(UpdatePostCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _repository.GetByIdAsync(request.Id);
        if (category == null) return false;

        category.Name = request.Name;
        category.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(category);
        return true;
    }
}
