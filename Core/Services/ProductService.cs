using Core.Entities;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Core.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<Product>> GetProductsAsync(string? category = null, decimal? minPrice = null, decimal? maxPrice = null, int? minRating = null, string? sortBy = null, bool ascending = true, int page = 1, int pageSize = 10)
    {
        var products = await _unitOfWork.Products.GetAllAsync();
        var productsList = products.ToList();

        if (!string.IsNullOrEmpty(category))
        {
            productsList = productsList.Where(p => 
                p.ProductCategories.Any(pc => pc.Category.Name.Equals(category, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        if (minPrice.HasValue)
        {
            productsList = productsList.Where(p => p.Price >= minPrice.Value).ToList();
        }

        if (maxPrice.HasValue)
        {
            productsList = productsList.Where(p => p.Price <= maxPrice.Value).ToList();
        }

        if (minRating.HasValue)
        {
            productsList = productsList.Where(p => 
                p.Reviews.Any() && p.Reviews.Average(r => r.Rating) >= minRating.Value
            ).ToList();
        }

        if (!string.IsNullOrEmpty(sortBy))
        {
            productsList = sortBy.ToLower() switch
            {
                "price" => ascending 
                    ? productsList.OrderBy(p => p.Price).ToList()
                    : productsList.OrderByDescending(p => p.Price).ToList(),
                "rating" => ascending
                    ? productsList.OrderBy(p => p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0).ToList()
                    : productsList.OrderByDescending(p => p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0).ToList(),
                "popularity" => ascending
                    ? productsList.OrderBy(p => p.OrderItems.Sum(oi => oi.Quantity)).ToList()
                    : productsList.OrderByDescending(p => p.OrderItems.Sum(oi => oi.Quantity)).ToList(),
                _ => productsList
            };
        }

        var skip = (page - 1) * pageSize;
        return productsList.Skip(skip).Take(pageSize);
    }

    public async Task<Product?> GetProductByIdAsync(Guid id)
    {
        return await _unitOfWork.Products.GetByIdAsync(id);
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        await _unitOfWork.Products.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();
        return product;
    }

    public async Task<Product?> UpdateProductAsync(Guid id, Product product)
    {
        var existingProduct = await _unitOfWork.Products.GetByIdAsync(id);
        if (existingProduct == null)
        {
            return null;
        }

        existingProduct.Name = product.Name;
        existingProduct.Description = product.Description;
        existingProduct.Price = product.Price;
        existingProduct.Stock = product.Stock;

        await _unitOfWork.Products.UpdateAsync(existingProduct);
        await _unitOfWork.SaveChangesAsync();

        return existingProduct;
    }

    public async Task<bool> DeleteProductAsync(Guid id)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product == null)
        {
            return false;
        }

        await _unitOfWork.Products.DeleteAsync(product);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}

