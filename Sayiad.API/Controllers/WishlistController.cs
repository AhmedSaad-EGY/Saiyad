using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sayiad.Domain.Dtos.WishlistDtos;

namespace Sayiad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WishlistController : ControllerBase
{
    private readonly IWishlistManager _wishlistManager;

    public WishlistController(IWishlistManager wishlistManager)
    {
        _wishlistManager = wishlistManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetWishlist()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var wishlist = await _wishlistManager.GetWishlistAsync(userId);
        return Ok(wishlist);
    }

    [HttpPost("toggle")]
    public async Task<IActionResult> Toggle(ToggleWishlistRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (item, added) = await _wishlistManager.ToggleAsync(userId, request);

        if (added)
            return Ok(item);

        return NoContent();
    }

    [HttpDelete("{productId}")]
    public async Task<IActionResult> Remove(int productId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _wishlistManager.RemoveAsync(userId, productId);
        return NoContent();
    }
}
