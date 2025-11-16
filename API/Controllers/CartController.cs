using API.DTOs;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Customer")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly IUnitOfWork _unitOfWork;

    public CartController(ICartService cartService, IUnitOfWork unitOfWork)
    {
        _cartService = cartService;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CartItemDto>>> GetCartItems()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var cartItems = await _cartService.GetCartItemsAsync(userId);
        var cartItemDtos = new List<CartItemDto>();

        foreach (var cartItem in cartItems)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(cartItem.ProductId);
            if (product != null)
            {
                cartItemDtos.Add(new CartItemDto
                {
                    Id = cartItem.Id,
                    ProductId = cartItem.ProductId,
                    ProductName = product.Name,
                    ProductPrice = product.Price,
                    Quantity = cartItem.Quantity,
                    Subtotal = product.Price * cartItem.Quantity
                });
            }
        }

        return Ok(cartItemDtos);
    }

    [HttpPost]
    public async Task<ActionResult<CartItemDto>> AddToCart([FromBody] AddToCartDto addToCartDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var cartItem = await _cartService.AddToCartAsync(userId, addToCartDto.ProductId, addToCartDto.Quantity);
            var product = await _unitOfWork.Products.GetByIdAsync(cartItem.ProductId);

            if (product == null)
            {
                return NotFound(new { error = "Product not found." });
            }

            var cartItemDto = new CartItemDto
            {
                Id = cartItem.Id,
                ProductId = cartItem.ProductId,
                ProductName = product.Name,
                ProductPrice = product.Price,
                Quantity = cartItem.Quantity,
                Subtotal = product.Price * cartItem.Quantity
            };

            return Ok(cartItemDto);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{productId}")]
    public async Task<ActionResult<CartItemDto>> UpdateCartItem(Guid productId, [FromBody] AddToCartDto updateCartDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var cartItem = await _cartService.UpdateCartItemAsync(userId, productId, updateCartDto.Quantity);
            if (cartItem == null)
            {
                return NotFound(new { error = "Cart item not found." });
            }

            var product = await _unitOfWork.Products.GetByIdAsync(cartItem.ProductId);
            if (product == null)
            {
                return NotFound(new { error = "Product not found." });
            }

            var cartItemDto = new CartItemDto
            {
                Id = cartItem.Id,
                ProductId = cartItem.ProductId,
                ProductName = product.Name,
                ProductPrice = product.Price,
                Quantity = cartItem.Quantity,
                Subtotal = product.Price * cartItem.Quantity
            };

            return Ok(cartItemDto);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{productId}")]
    public async Task<IActionResult> RemoveFromCart(Guid productId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var removed = await _cartService.RemoveFromCartAsync(userId, productId);
        if (!removed)
        {
            return NotFound(new { error = "Cart item not found." });
        }

        return NoContent();
    }
}

