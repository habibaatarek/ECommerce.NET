using Core.Entities;

namespace Core.Interfaces;

public interface IOrderService
{
    Task<IEnumerable<Order>> GetUserOrdersAsync(Guid userId);
    Task<Order?> GetOrderByIdAsync(Guid id, Guid userId);
    Task<Order> CreateOrderFromCartAsync(Guid userId);
    Task<bool> UpdateOrderStatusAsync(Guid id, string status);
}

