namespace Sayiad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[RequestSizeLimit(1 * 1024 * 1024)]
public class ReviewsController : BaseController
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

    [Authorize(Roles = $"{nameof(UserRole.Customer)},{nameof(UserRole.Fisherman)},{nameof(UserRole.BaitSeller)},{nameof(UserRole.Auctioneer)}")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateReviewRequest request)
    {
        var userId = GetUserId();
        var review = await _reviewManager.CreateAsync(userId, request);
        return CreatedAtAction(nameof(GetProductReviews), new { productId = request.ProductId }, review);
    }

    [Authorize(Roles = $"{nameof(UserRole.Customer)},{nameof(UserRole.Fisherman)},{nameof(UserRole.BaitSeller)},{nameof(UserRole.Auctioneer)}")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        await _reviewManager.DeleteAsync(id, userId);
        return NoContent();
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpDelete("{id}/admin")]
    public async Task<IActionResult> AdminDeleteReview(int id, [FromBody] AdminDeleteReviewRequest request)
    {
        await _reviewManager.AdminDeleteAsync(id, request.Reason);
        return NoContent();
    }
}
