namespace Core.Entities;

public class Review
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Rating { get; set; } // 1-5
    public string Comment { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Product Product { get; set; } = null!;
    public User User { get; set; } = null!;
}

