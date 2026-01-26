using BlogApi.Application.Common.Interfaces;
using BlogApi.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UUIDNext;

namespace BlogApi.Application.Features.Cart.Commands;

public record AddToCartCommand(Guid ProductId, int Quantity) : IRequest<Unit>;

public class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, Unit>
{
    private readonly IGenericRepository<BlogApi.Domain.Entities.Cart, Guid> _cartRepository;
    private readonly IGenericRepository<CartItem, Guid> _cartItemRepository;
    private readonly ICurrentUserService _currentUserService;

    public AddToCartCommandHandler(
        IGenericRepository<BlogApi.Domain.Entities.Cart, Guid> cartRepository, 
        IGenericRepository<CartItem, Guid> cartItemRepository,
        ICurrentUserService currentUserService)
    {
        _cartRepository = cartRepository;
        _cartItemRepository = cartItemRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new Exception("User not authenticated");

        // 1. Check or Create Cart (Cart Table)
        var cart = await _cartRepository.GetQueryable()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart == null)
        {
            if (request.Quantity <= 0) return Unit.Value; // Nothing to add/reduce

            cart = new BlogApi.Domain.Entities.Cart 
            { 
                Id = Uuid.NewDatabaseFriendly(Database.SqlServer),
                UserId = userId 
            };
            await _cartRepository.AddAsync(cart);
        }

        // 2. Manage CartItem (CartItem Table)
        var existingItem = await _cartItemRepository.GetQueryable()
            .FirstOrDefaultAsync(i => i.CartId == cart.Id && i.ProductId == request.ProductId, cancellationToken);

        if (existingItem != null)
        {
            existingItem.Quantity += request.Quantity;
            if (existingItem.Quantity <= 0)
            {
                await _cartItemRepository.DeleteAsync(existingItem);
            }
            else
            {
                await _cartItemRepository.UpdateAsync(existingItem);
            }
        }
        else if (request.Quantity > 0)
        {
            var newItem = new CartItem
            {
                Id = Uuid.NewDatabaseFriendly(Database.SqlServer),
                CartId = cart.Id,
                ProductId = request.ProductId,
                Quantity = request.Quantity
            };
            await _cartItemRepository.AddAsync(newItem);
        }

        return Unit.Value;
    }
}
