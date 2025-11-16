using API.DTOs;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public ReviewController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("products/{productId}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ReviewDto>>> GetProductReviews(Guid productId)
    {
        var reviews = await _unitOfWork.Reviews.FindAsync(r => r.ProductId == productId);
        var reviewDtos = reviews.Select(r => new ReviewDto
        {
            Id = r.Id,
            Rating = r.Rating,
            Comment = r.Comment,
            ProductId = r.ProductId,
            UserId = r.UserId,
            UserEmail = r.User.Email,
            CreatedAt = r.CreatedAt
        });

        return Ok(reviewDtos);
    }

    [HttpPost("products/{productId}")]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<ReviewDto>> CreateReview(Guid productId, [FromBody] CreateReviewDto createReviewDto)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        if (product == null)
        {
            return NotFound(new { error = "Product not found." });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        // check if user already reviewed this product
        var existingReview = await _unitOfWork.Reviews.FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);
        if (existingReview != null)
        {
            return Conflict(new { error = "You have already reviewed this product." });
        }

        var review = new Review
        {
            Rating = createReviewDto.Rating,
            Comment = createReviewDto.Comment,
            ProductId = productId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Reviews.AddAsync(review);
        await _unitOfWork.SaveChangesAsync();

        var reloadedReview = await _unitOfWork.Reviews.GetByIdAsync(review.Id);
        if (reloadedReview == null)
        {
            return BadRequest(new { error = "Failed to reload review." });
        }

        var reviewDto = new ReviewDto
        {
            Id = reloadedReview.Id,
            Rating = reloadedReview.Rating,
            Comment = reloadedReview.Comment,
            ProductId = reloadedReview.ProductId,
            UserId = reloadedReview.UserId,
            UserEmail = reloadedReview.User.Email,
            CreatedAt = reloadedReview.CreatedAt
        };

        return CreatedAtAction(nameof(GetProductReviews), new { productId = productId }, reviewDto);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<ReviewDto>> UpdateReview(Guid id, [FromBody] CreateReviewDto updateReviewDto)
    {
        var review = await _unitOfWork.Reviews.GetByIdAsync(id);
        if (review == null)
        {
            return NotFound(new { error = "Review not found." });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        if (review.UserId != userId)
        {
            return Forbid();
        }

        review.Rating = updateReviewDto.Rating;
        review.Comment = updateReviewDto.Comment;

        await _unitOfWork.Reviews.UpdateAsync(review);
        await _unitOfWork.SaveChangesAsync();

        var reviewDto = new ReviewDto
        {
            Id = review.Id,
            Rating = review.Rating,
            Comment = review.Comment,
            ProductId = review.ProductId,
            UserId = review.UserId,
            UserEmail = review.User.Email,
            CreatedAt = review.CreatedAt
        };

        return Ok(reviewDto);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Customer,Admin")]
    public async Task<IActionResult> DeleteReview(Guid id)
    {
        var review = await _unitOfWork.Reviews.GetByIdAsync(id);
        if (review == null)
        {
            return NotFound(new { error = "Review not found." });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        if (userRole != "Admin" && review.UserId != userId)
        {
            return Forbid();
        }

        await _unitOfWork.Reviews.DeleteAsync(review);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }
}

