using FloraCore.Application.Common.Interfaces;
using FloraCore.Domain.Entities;
using System.Threading.Tasks;

namespace FloraCore.Application.Interfaces;

public interface IOrderRepository : IGenericRepository<Order, Guid>
{
    Task AddOrderItemAsync(OrderItem orderItem);
    Task DeleteOrderItemAsync(OrderItem orderItem);
}
