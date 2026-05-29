using FloraCore.Application.Interfaces;
using FloraCore.Domain.Entities;
using FloraCore.Infrastructure.Data;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;

namespace FloraCore.Infrastructure.Repositories;

public class OrderRepository(AppDbContext context) : GenericRepository<Order, Guid>(context ?? throw new ArgumentNullException(nameof(context))), IOrderRepository
{
    private readonly AppDbContext _dbContext = context ?? throw new ArgumentNullException(nameof(context));

    public async Task AddOrderItemAsync(OrderItem orderItem)
    {
        await _dbContext.OrderItems.AddAsync(orderItem);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteOrderItemAsync(OrderItem orderItem)
    {
        _dbContext.OrderItems.Remove(orderItem);
        await _dbContext.SaveChangesAsync();
    }
}
