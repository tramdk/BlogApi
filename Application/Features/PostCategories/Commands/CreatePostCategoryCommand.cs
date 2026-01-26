using BlogApi.Domain.Entities;
using BlogApi.Application.Common.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace BlogApi.Application.Features.PostCategories.Commands;

public record CreatePostCategoryCommand(string Id, string Name) : IRequest<string>;

public class CreatePostCategoryCommandHandler : IRequestHandler<CreatePostCategoryCommand, string>
{
    private readonly IGenericRepository<PostCategory, string> _repository;

    public CreatePostCategoryCommandHandler(IGenericRepository<PostCategory, string> repository)
    {
        _repository = repository;
    }

    public async Task<string> Handle(CreatePostCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = new PostCategory
        {
            Id = request.Id,
            Name = request.Name
        };

        await _repository.AddAsync(category);
        return category.Id;
    }
}
