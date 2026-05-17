using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sayiad.Domain.Contracts.Subscription;
using Sayiad.Domain.Dtos.Subscription;

namespace Sayiad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionManager _subscriptionManager;

    public SubscriptionsController(ISubscriptionManager subscriptionManager)
    {
        _subscriptionManager = subscriptionManager;
    }

    [HttpPost("upgrade")]
    public async Task<IActionResult> Upgrade(UpgradeSubscriptionRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _subscriptionManager.UpgradeAsync(userId, request);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMySubscription()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _subscriptionManager.GetMySubscriptionAsync(userId);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Data);
    }

    [Authorize(Roles = "Admin")]
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
