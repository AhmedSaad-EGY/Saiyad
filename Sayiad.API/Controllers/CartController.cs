namespace Sayiad.Api.Controllers;

[ApiController]
    [Route("api/[controller]")]
[Authorize(Roles = $"{nameof(UserRole.Customer)},{nameof(UserRole.Fisherman)},{nameof(UserRole.BaitSeller)},{nameof(UserRole.Auctioneer)}")]
public class CartController : BaseController
{
    private readonly ICartManager _cartManager;

    public CartController(ICartManager cartManager)
    {
        _cartManager = cartManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var userId = GetUserId();
        var cart = await _cartManager.GetCartAsync(userId);
        return Ok(cart);
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem(AddToCartRequest request)
    {
        var userId = GetUserId();
        var cart = await _cartManager.AddItemAsync(userId, request);
        return Created("", cart);
    }

    [HttpPut("items/{productId}")]
    public async Task<IActionResult> UpdateItem(int productId, UpdateCartItemRequest request)
    {
        var userId = GetUserId();
        var cart = await _cartManager.UpdateItemQuantityAsync(userId, productId, request);
        return Ok(cart);
    }

    [HttpDelete("items/{productId}")]
    public async Task<IActionResult> RemoveItem(int productId)
    {
        var userId = GetUserId();
        await _cartManager.RemoveItemAsync(userId, productId);
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> Clear()
    {
        var userId = GetUserId();
        await _cartManager.ClearCartAsync(userId);
        return NoContent();
    }
}
