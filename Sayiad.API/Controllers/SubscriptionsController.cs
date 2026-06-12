namespace Sayiad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionsController : BaseController
{
    private readonly ISubscriptionManager _subscriptionManager;

    public SubscriptionsController(ISubscriptionManager subscriptionManager)
    {
        _subscriptionManager = subscriptionManager;
    }

    [Authorize(Roles = $"{nameof(UserRole.Customer)},{nameof(UserRole.Fisherman)},{nameof(UserRole.BaitSeller)},{nameof(UserRole.Auctioneer)}")]
    [HttpPost("upgrade")]
    public async Task<IActionResult> Upgrade(UpgradeSubscriptionRequest request)
    {
        var userId = GetUserId();
        var result = await _subscriptionManager.UpgradeAsync(userId, request);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [Authorize(Roles = $"{nameof(UserRole.Customer)},{nameof(UserRole.Fisherman)},{nameof(UserRole.BaitSeller)},{nameof(UserRole.Auctioneer)}")]
    [HttpGet("my")]
    public async Task<IActionResult> GetMySubscription()
    {
        var userId = GetUserId();
        var result = await _subscriptionManager.GetMySubscriptionAsync(userId);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var pagination = new PaginationRequest { Page = page, PageSize = pageSize };
        var result = await _subscriptionManager.GetAllAsync(pagination);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }
}
