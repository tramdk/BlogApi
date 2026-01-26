using BlogApi.Domain.Entities;
using BlogApi.Application.Common.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlogApi.Application.Features.ProductCategories.Commands;

public record DeleteProductCategoryCommand(Guid Id) : IRequest<bool>;

public class DeleteProductCategoryCommandHandler : IRequestHandler<DeleteProductCategoryCommand, bool>
{
    private readonly IGenericRepository<ProductCategory, Guid> _repository;

    public DeleteProductCategoryCommandHandler(IGenericRepository<ProductCategory, Guid> repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(DeleteProductCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _repository.GetByIdAsync(request.Id);
        if (category == null) return false;

        await _repository.DeleteAsync(category);
        return true;
    }
}
