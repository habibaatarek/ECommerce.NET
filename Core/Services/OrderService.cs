using Core.Entities;
using Core.Interfaces;

namespace Core.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<Order>> GetUserOrdersAsync(Guid userId)
    {
        var orders = await _unitOfWork.Orders.FindAsync(o => o.UserId == userId);
        return orders.OrderByDescending(o => o.OrderDate);
    }

    public async Task<Order?> GetOrderByIdAsync(Guid id, Guid userId)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(id);
        if (order == null || order.UserId != userId)
        {
            return null;
        }
        return order;
    }

    public async Task<Order> CreateOrderFromCartAsync(Guid userId)
    {
        var cartItems = await _unitOfWork.CartItems.FindAsync(ci => ci.UserId == userId);
        var cartItemsList = cartItems.ToList();

        if (!cartItemsList.Any())
        {
            throw new InvalidOperationException("Cart is empty.");
        }
        
        decimal totalAmount = 0;
        var orderItems = new List<OrderItem>();

        foreach (var cartItem in cartItemsList)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(cartItem.ProductId);
            if (product == null || product.Stock < cartItem.Quantity)
            {
                throw new InvalidOperationException($"Insufficient stock for product {product?.Name ?? "Unknown"}.");
            }

            var itemTotal = product.Price * cartItem.Quantity;
            totalAmount += itemTotal;

            orderItems.Add(new OrderItem
            {
                ProductId = cartItem.ProductId,
                Quantity = cartItem.Quantity,
                UnitPrice = product.Price
            });

            product.Stock -= cartItem.Quantity;
            await _unitOfWork.Products.UpdateAsync(product);
        }

        var order = new Order
        {
            UserId = userId,
            OrderDate = DateTime.UtcNow,
            TotalAmount = totalAmount,
            Status = "Pending"
        };

        await _unitOfWork.Orders.AddAsync(order);
        await _unitOfWork.SaveChangesAsync();

        foreach (var orderItem in orderItems)
        {
            orderItem.OrderId = order.Id;
            await _unitOfWork.OrderItems.AddAsync(orderItem);
        }

        foreach (var cartItem in cartItemsList)
        {
            await _unitOfWork.CartItems.DeleteAsync(cartItem);
        }

        await _unitOfWork.SaveChangesAsync();
        return order;
    }

    public async Task<bool> UpdateOrderStatusAsync(Guid id, string status)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(id);
        if (order == null)
        {
            return false;
        }

        if (!new[] { "Pending", "Paid", "Shipped", "Delivered" }.Contains(status))
        {
            throw new ArgumentException("Invalid status.");
        }

        order.Status = status;
        await _unitOfWork.Orders.UpdateAsync(order);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}

