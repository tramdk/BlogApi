using BlogApi.Domain.Entities;
using BlogApi.Application.Common.Interfaces;
using MediatR;
using UUIDNext;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlogApi.Application.Features.Products.Commands;

public record CreateProductCommand(string Name, string Description, decimal Price, int Stock, string ImageUrl, Guid? CategoryId) : IRequest<Guid>;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly IGenericRepository<Product, Guid> _productRepository;

    public CreateProductCommandHandler(IGenericRepository<Product, Guid> productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Id = Uuid.NewDatabaseFriendly(Database.SqlServer),
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Stock = request.Stock,
            ImageUrl = request.ImageUrl,
            CategoryId = request.CategoryId
        };

        await _productRepository.AddAsync(product);
        return product.Id;
    }
}
