using Core.Entities;
using Core.Interfaces;

namespace Core.Services;

public class CartService : ICartService
{
    private readonly IUnitOfWork _unitOfWork;

    public CartService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<CartItem>> GetCartItemsAsync(Guid userId)
    {
        return await _unitOfWork.CartItems.FindAsync(ci => ci.UserId == userId);
    }

    public async Task<CartItem> AddToCartAsync(Guid userId, Guid productId, int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than 0.");
        }

        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        if (product == null)
        {
            throw new InvalidOperationException("Product not found.");
        }

        if (product.Stock < quantity)
        {
            throw new InvalidOperationException("Insufficient stock.");
        }

        var existingCartItem = await _unitOfWork.CartItems.FirstOrDefaultAsync(
            ci => ci.UserId == userId && ci.ProductId == productId
        );

        if (existingCartItem != null)
        {
            existingCartItem.Quantity += quantity;
            if (product.Stock < existingCartItem.Quantity)
            {
                throw new InvalidOperationException("Insufficient stock.");
            }
            await _unitOfWork.CartItems.UpdateAsync(existingCartItem);
            await _unitOfWork.SaveChangesAsync();
            return existingCartItem;
        }

        var cartItem = new CartItem
        {
            UserId = userId,
            ProductId = productId,
            Quantity = quantity
        };

        await _unitOfWork.CartItems.AddAsync(cartItem);
        await _unitOfWork.SaveChangesAsync();
        return cartItem;
    }

    public async Task<CartItem?> UpdateCartItemAsync(Guid userId, Guid productId, int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than 0.");
        }

        var cartItem = await _unitOfWork.CartItems.FirstOrDefaultAsync(
            ci => ci.UserId == userId && ci.ProductId == productId
        );

        if (cartItem == null)
        {
            return null;
        }

        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        if (product == null || product.Stock < quantity)
        {
            throw new InvalidOperationException("Insufficient stock.");
        }

        cartItem.Quantity = quantity;
        await _unitOfWork.CartItems.UpdateAsync(cartItem);
        await _unitOfWork.SaveChangesAsync();
        return cartItem;
    }

    public async Task<bool> RemoveFromCartAsync(Guid userId, Guid productId)
    {
        var cartItem = await _unitOfWork.CartItems.FirstOrDefaultAsync(
            ci => ci.UserId == userId && ci.ProductId == productId
        );

        if (cartItem == null)
        {
            return false;
        }

        await _unitOfWork.CartItems.DeleteAsync(cartItem);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ClearCartAsync(Guid userId)
    {
        var cartItems = await _unitOfWork.CartItems.FindAsync(ci => ci.UserId == userId);
        foreach (var cartItem in cartItems)
        {
            await _unitOfWork.CartItems.DeleteAsync(cartItem);
        }
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}

