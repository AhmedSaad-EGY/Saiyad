namespace Sayiad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{nameof(UserRole.Customer)},{nameof(UserRole.Fisherman)},{nameof(UserRole.BaitSeller)},{nameof(UserRole.Auctioneer)}")]
public class WishlistController : BaseController
{
    private readonly IWishlistManager _wishlistManager;

    public WishlistController(IWishlistManager wishlistManager)
    {
        _wishlistManager = wishlistManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetWishlist([FromQuery] PaginationRequest? pagination)
    {
        var userId = GetUserId();
        var p = pagination ?? new PaginationRequest();

        if (p.PageSize == int.MaxValue)
        {
            var wishlist = await _wishlistManager.GetWishlistAsync(userId);
            return Ok(wishlist);
        }

        var result = await _wishlistManager.GetWishlistPagedAsync(userId, p);
        return Ok(result);
    }

    [HttpPost("toggle")]
    public async Task<IActionResult> Toggle(ToggleWishlistRequest request)
    {
        var userId = GetUserId();
        var (item, added) = await _wishlistManager.ToggleAsync(userId, request);

        if (added)
            return Ok(item);

        return NoContent();
    }

    [HttpDelete("{productId}")]
    public async Task<IActionResult> Remove(int productId)
    {
        var userId = GetUserId();
        await _wishlistManager.RemoveAsync(userId, productId);
        return NoContent();
    }
}
