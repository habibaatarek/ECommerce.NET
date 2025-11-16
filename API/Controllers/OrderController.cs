using API.DTOs;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IUnitOfWork _unitOfWork;

    public OrderController(IOrderService orderService, IUnitOfWork unitOfWork)
    {
        _orderService = orderService;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var orders = await _orderService.GetUserOrdersAsync(userId);
        var orderDtos = new List<OrderDto>();

        foreach (var order in orders)
        {
            var orderItems = await _unitOfWork.OrderItems.FindAsync(oi => oi.OrderId == order.Id);
            var orderItemDtos = new List<OrderItemDto>();

            foreach (var orderItem in orderItems)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(orderItem.ProductId);
                orderItemDtos.Add(new OrderItemDto
                {
                    Id = orderItem.Id,
                    ProductId = orderItem.ProductId,
                    ProductName = product?.Name ?? "Unknown",
                    Quantity = orderItem.Quantity,
                    UnitPrice = orderItem.UnitPrice,
                    Subtotal = orderItem.Quantity * orderItem.UnitPrice
                });
            }

            orderDtos.Add(new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                UserEmail = order.User.Email,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                OrderItems = orderItemDtos
            });
        }

        return Ok(orderDtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetOrder(Guid id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var order = await _orderService.GetOrderByIdAsync(id, userId);
        if (order == null && userRole != "Admin")
        {
            return NotFound(new { error = "Order not found." });
        }

        if (order == null && userRole == "Admin")
        {
            order = await _unitOfWork.Orders.GetByIdAsync(id);
            if (order == null)
            {
                return NotFound(new { error = "Order not found." });
            }
        }

        if (order == null)
        {
            return NotFound(new { error = "Order not found." });
        }

        var orderItems = await _unitOfWork.OrderItems.FindAsync(oi => oi.OrderId == order.Id);
        var orderItemDtos = new List<OrderItemDto>();

        foreach (var orderItem in orderItems)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(orderItem.ProductId);
            orderItemDtos.Add(new OrderItemDto
            {
                Id = orderItem.Id,
                ProductId = orderItem.ProductId,
                ProductName = product?.Name ?? "Unknown",
                Quantity = orderItem.Quantity,
                UnitPrice = orderItem.UnitPrice,
                Subtotal = orderItem.Quantity * orderItem.UnitPrice
            });
        }

        var orderDto = new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            UserEmail = order.User?.Email ?? "Unknown",
            OrderDate = order.OrderDate,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            OrderItems = orderItemDtos
        };

        return Ok(orderDto);
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<OrderDto>> CreateOrder()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var order = await _orderService.CreateOrderFromCartAsync(userId);

            var orderItems = await _unitOfWork.OrderItems.FindAsync(oi => oi.OrderId == order.Id);
            var orderItemDtos = new List<OrderItemDto>();

            foreach (var orderItem in orderItems)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(orderItem.ProductId);
                orderItemDtos.Add(new OrderItemDto
                {
                    Id = orderItem.Id,
                    ProductId = orderItem.ProductId,
                    ProductName = product?.Name ?? "Unknown",
                    Quantity = orderItem.Quantity,
                    UnitPrice = orderItem.UnitPrice,
                    Subtotal = orderItem.Quantity * orderItem.UnitPrice
                });
            }

            var orderDto = new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                UserEmail = order.User.Email,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                OrderItems = orderItemDtos
            };

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, orderDto);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusDto updateOrderStatusDto)
    {
        try
        {
            var updated = await _orderService.UpdateOrderStatusAsync(id, updateOrderStatusDto.Status);
            if (!updated)
            {
                return NotFound(new { error = "Order not found." });
            }

            return Ok(new { message = "Order status updated successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

