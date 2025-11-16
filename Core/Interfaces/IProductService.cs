using Core.Entities;

namespace Core.Interfaces;

public interface IProductService
{
    Task<IEnumerable<Product>> GetProductsAsync(string? category = null, decimal? minPrice = null, decimal? maxPrice = null, int? minRating = null, string? sortBy = null, bool ascending = true, int page = 1, int pageSize = 10);
    Task<Product?> GetProductByIdAsync(Guid id);
    Task<Product> CreateProductAsync(Product product);
    Task<Product?> UpdateProductAsync(Guid id, Product product);
    Task<bool> DeleteProductAsync(Guid id);
}

