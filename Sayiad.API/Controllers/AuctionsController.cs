using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Sayiad.Api.Hubs;
using Sayiad.Domain.Dtos.AuctionDtos;

namespace Sayiad.Api.Controllers;

/// <summary>
/// Manages auctions: listing active auctions, viewing details,
/// creating auctions, placing bids, and ending auctions.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[RequestSizeLimit(5 * 1024 * 1024)]
public class AuctionsController : ControllerBase
{
    private readonly IAuctionManager _auctionManager;
    private readonly IHubContext<AuctionHub> _hubContext;

    public AuctionsController(IAuctionManager auctionManager, IHubContext<AuctionHub> hubContext)
    {
        _auctionManager = auctionManager;
        _hubContext = hubContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetActive([FromQuery] AuctionFilterRequest? filter, [FromQuery] PaginationRequest? pagination)
    {
        var auctions = await _auctionManager.GetActiveAsync(filter, pagination);
        return Ok(auctions);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var auction = await _auctionManager.GetByIdAsync(id);
        return Ok(auction);
    }

    [Authorize(Roles = nameof(UserRole.Auctioneer))]
    [HttpPost]
    public async Task<IActionResult> Create(CreateAuctionRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var auction = await _auctionManager.CreateAsync(userId, request);
        return CreatedAtAction(nameof(GetById), new { id = auction.Id }, auction);
    }

    /// <summary>
    /// Places a bid on an active auction. Supports optional auto-bid via MaxAutoBidAmount.
    /// Broadcasts the bid to all connected SignalR clients in the auction group.
    /// </summary>
    [Authorize(Roles = $"{nameof(UserRole.Customer)},{nameof(UserRole.Admin)}")]
    [HttpPost("{id}/bids")]
    public async Task<IActionResult> PlaceBid(int id, PlaceBidRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var bid = await _auctionManager.PlaceBidAsync(id, userId, request);
        await _hubContext.Clients.Group($"auction-{id}").SendAsync("BidPlaced", bid);
        return Created("", bid);
    }

    [Authorize(Roles = $"{nameof(UserRole.Auctioneer)},{nameof(UserRole.Admin)}")]
    [HttpPost("{id}/end")]
    public async Task<IActionResult> EndAuction(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var auction = await _auctionManager.EndAuctionAsync(id, userId);
        await _hubContext.Clients.Group($"auction-{id}").SendAsync("AuctionEnded", auction);
        return Ok(auction);
    }

    // ── FISHERMAN: Auction requests ────────────────────────────────

    [Authorize(Roles = nameof(UserRole.Fisherman))]
    [HttpPost("requests")]
    public async Task<IActionResult> SubmitRequest(SubmitAuctionRequestRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _auctionManager.SubmitRequestAsync(userId, request);
        return CreatedAtAction(nameof(GetMyRequests), null, result);
    }

    [Authorize(Roles = nameof(UserRole.Fisherman))]
    [HttpGet("requests/my")]
    public async Task<IActionResult> GetMyRequests([FromQuery] PaginationRequest? pagination)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _auctionManager.GetMyRequestsAsync(userId, pagination ?? new PaginationRequest());
        return Ok(result);
    }

    // ── AUCTIONEER: Review auction requests ───────────────────────

    [Authorize(Roles = nameof(UserRole.Auctioneer))]
    [HttpGet("requests/pending")]
    public async Task<IActionResult> GetPendingRequests([FromQuery] PaginationRequest? pagination)
    {
        var result = await _auctionManager.GetPendingRequestsAsync(pagination ?? new PaginationRequest());
        return Ok(result);
    }

    [Authorize(Roles = nameof(UserRole.Auctioneer))]
    [HttpPost("requests/{id}/approve")]
    public async Task<IActionResult> ApproveRequest(int id, ApproveAuctionRequestRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var auction = await _auctionManager.ApproveRequestAsync(id, userId, request);
        return CreatedAtAction(nameof(GetById), new { id = auction.Id }, auction);
    }

    [Authorize(Roles = nameof(UserRole.Auctioneer))]
    [HttpPost("requests/{id}/reject")]
    public async Task<IActionResult> RejectRequest(int id, RejectAuctionRequestRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _auctionManager.RejectRequestAsync(id, userId, request);
        return Ok(result);
    }

    // ── AUCTIONEER: Analytics dashboard ───────────────────────────

    [Authorize(Roles = $"{nameof(UserRole.Auctioneer)},{nameof(UserRole.Admin)}")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetAuctioneerDashboard()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var dashboard = await _auctionManager.GetAuctioneerDashboardAsync(userId);
        return Ok(dashboard);
    }
}
