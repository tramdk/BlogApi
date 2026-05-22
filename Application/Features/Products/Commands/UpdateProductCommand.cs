using FloraCore.Domain.Entities;
using FloraCore.Application.Common.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FloraCore.Application.Features.Products.Commands;

public record UpdateProductCommand(Guid Id, string Name, string Description, decimal Price, /// <summary>
    /// The promotion rate of the product.
    /// </summary>
		decimal PromotionRate, int Stock, string? ImageUrl, Guid? CategoryId) : IRequest<Unit>;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Unit>
{
    private readonly IGenericRepository<Product, Guid> _productRepository;

    public UpdateProductCommandHandler(IGenericRepository<Product, Guid> productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Unit> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id);
        if (product == null) throw new Exception("Product not found");

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
				product.PromotionRate = request.PromotionRate;
        product.Stock = request.Stock;
        product.ImageUrl = request.ImageUrl;
        product.CategoryId = request.CategoryId;
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepository.UpdateAsync(product);
        return Unit.Value;
    }
}
