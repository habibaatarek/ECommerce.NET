using Core.Entities;

namespace Core.Interfaces;

public interface ICartService
{
    Task<IEnumerable<CartItem>> GetCartItemsAsync(Guid userId);
    Task<CartItem> AddToCartAsync(Guid userId, Guid productId, int quantity);
    Task<CartItem?> UpdateCartItemAsync(Guid userId, Guid productId, int quantity);
    Task<bool> RemoveFromCartAsync(Guid userId, Guid productId);
    Task<bool> ClearCartAsync(Guid userId);
}

