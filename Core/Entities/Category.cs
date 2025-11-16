namespace Core.Entities;

public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;

    public ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();
}

