using FloraCore.Application.Interfaces;
using FloraCore.Domain.Entities;
using FloraCore.Infrastructure.Data;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;

namespace FloraCore.Infrastructure.Repositories;

public class OrderRepository(AppDbContext context) : GenericRepository<Order, Guid>(context), IOrderRepository
{
    private readonly AppDbContext _dbContext = context ?? throw new ArgumentNullException(nameof(context));

    public async Task AddOrderItemAsync(OrderItem orderItem)
    {
        await _context.OrderItems.AddAsync(orderItem);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteOrderItemAsync(OrderItem orderItem)
    {
        _context.OrderItems.Remove(orderItem);
        await _context.SaveChangesAsync();
    }

    public override async Task DeleteAsync(Order entity)
    {
        _context.Orders.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
