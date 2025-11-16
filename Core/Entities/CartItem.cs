namespace Core.Entities;

public class CartItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }

    public User User { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

