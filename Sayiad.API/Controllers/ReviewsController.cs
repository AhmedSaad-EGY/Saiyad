using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sayiad.Domain.Dtos.ReviewDtos;

namespace Sayiad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[RequestSizeLimit(1 * 1024 * 1024)]
public class ReviewsController : ControllerBase
{
    private readonly IReviewManager _reviewManager;

    public ReviewsController(IReviewManager reviewManager)
    {
        _reviewManager = reviewManager;
    }

    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetProductReviews(int productId)
    {
        var reviews = await _reviewManager.GetProductReviewsAsync(productId);
        return Ok(reviews);
    }

    [HttpGet("product/{productId}/rating")]
    public async Task<IActionResult> GetProductRating(int productId)
    {
        var rating = await _reviewManager.GetProductRatingAsync(productId);
        return Ok(rating);
    }

    [Authorize(Roles = $"{nameof(UserRole.Customer)},{nameof(UserRole.Fisherman)},{nameof(UserRole.BaitSeller)}")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateReviewRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var review = await _reviewManager.CreateAsync(userId, request);
        return CreatedAtAction(nameof(GetProductReviews), new { productId = request.ProductId }, review);
    }

    [Authorize(Roles = $"{nameof(UserRole.Customer)},{nameof(UserRole.Fisherman)},{nameof(UserRole.BaitSeller)}")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _reviewManager.DeleteAsync(id, userId);
        return NoContent();
    }
}
