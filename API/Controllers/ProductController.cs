using API.DTOs;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IUnitOfWork _unitOfWork;

    public ProductController(IProductService productService, IUnitOfWork unitOfWork)
    {
        _productService = productService;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResponseDto<ProductDto>>> GetProducts(
        [FromQuery] string? category = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] int? minRating = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool ascending = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var products = await _productService.GetProductsAsync(category, minPrice, maxPrice, minRating, sortBy, ascending, page, pageSize);
        var productsList = products.ToList();

        var productDtos = productsList.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            Stock = p.Stock,
            SellerId = p.SellerId,
            SellerEmail = p.Seller.Email,
            CreatedAt = p.CreatedAt,
            Categories = p.ProductCategories.Select(pc => pc.Category.Name).ToList(),
            AverageRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : null,
            ReviewCount = p.Reviews.Count
        });

        var totalCount = await _unitOfWork.Products.CountAsync();
        var response = new PagedResponseDto<ProductDto>
        {
            Data = productDtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Ok(response);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductDto>> GetProduct(Guid id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null)
        {
            return NotFound(new { error = "Product not found." });
        }

        var productDto = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Stock,
            SellerId = product.SellerId,
            SellerEmail = product.Seller.Email,
            CreatedAt = product.CreatedAt,
            Categories = product.ProductCategories.Select(pc => pc.Category.Name).ToList(),
            AverageRating = product.Reviews.Any() ? product.Reviews.Average(r => r.Rating) : null,
            ReviewCount = product.Reviews.Count
        };

        return Ok(productDto);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Seller")]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductDto createProductDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var product = new Product
        {
            Name = createProductDto.Name,
            Description = createProductDto.Description,
            Price = createProductDto.Price,
            Stock = createProductDto.Stock,
            SellerId = userId,
            CreatedAt = DateTime.UtcNow
        };

        var createdProduct = await _productService.CreateProductAsync(product);

        foreach (var categoryId in createProductDto.CategoryIds)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(categoryId);
            if (category != null)
            {
                await _unitOfWork.ProductCategories.AddAsync(new ProductCategory
                {
                    ProductId = createdProduct.Id,
                    CategoryId = categoryId
                });
            }
        }

        await _unitOfWork.SaveChangesAsync();

        var reloadedProduct = await _productService.GetProductByIdAsync(createdProduct.Id);
        if (reloadedProduct == null)
        {
            return BadRequest(new { error = "Failed to reload product." });
        }

        var productDto = new ProductDto
        {
            Id = reloadedProduct.Id,
            Name = reloadedProduct.Name,
            Description = reloadedProduct.Description,
            Price = reloadedProduct.Price,
            Stock = reloadedProduct.Stock,
            SellerId = reloadedProduct.SellerId,
            SellerEmail = reloadedProduct.Seller.Email,
            CreatedAt = reloadedProduct.CreatedAt,
            Categories = reloadedProduct.ProductCategories.Select(pc => pc.Category.Name).ToList()
        };

        return CreatedAtAction(nameof(GetProduct), new { id = reloadedProduct.Id }, productDto);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Seller")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(Guid id, [FromBody] UpdateProductDto updateProductDto)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null)
        {
            return NotFound(new { error = "Product not found." });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        if (userRole != "Admin" && product.SellerId != userId)
        {
            return Forbid();
        }

        var updatedProduct = new Product
        {
            Name = updateProductDto.Name,
            Description = updateProductDto.Description,
            Price = updateProductDto.Price,
            Stock = updateProductDto.Stock
        };

        var result = await _productService.UpdateProductAsync(id, updatedProduct);
        if (result == null)
        {
            return NotFound(new { error = "Product not found." });
        }

        var productDto = new ProductDto
        {
            Id = result.Id,
            Name = result.Name,
            Description = result.Description,
            Price = result.Price,
            Stock = result.Stock,
            SellerId = result.SellerId,
            SellerEmail = result.Seller.Email,
            CreatedAt = result.CreatedAt,
            Categories = result.ProductCategories.Select(pc => pc.Category.Name).ToList()
        };

        return Ok(productDto);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Seller")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null)
        {
            return NotFound(new { error = "Product not found." });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        if (userRole != "Admin" && product.SellerId != userId)
        {
            return Forbid();
        }

        var deleted = await _productService.DeleteProductAsync(id);
        if (!deleted)
        {
            return NotFound(new { error = "Product not found." });
        }

        return NoContent();
    }
}

