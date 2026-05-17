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

    [Authorize(Roles = $"{nameof(UserRole.Auctioneer)},{nameof(UserRole.Fisherman)},{nameof(UserRole.BaitSeller)}")]
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
    [Authorize(Roles = $"{nameof(UserRole.Customer)},{nameof(UserRole.Fisherman)},{nameof(UserRole.BaitSeller)}")]
    [HttpPost("{id}/bids")]
    public async Task<IActionResult> PlaceBid(int id, PlaceBidRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var bid = await _auctionManager.PlaceBidAsync(id, userId, request);
        await _hubContext.Clients.Group($"auction-{id}").SendAsync("BidPlaced", bid);
        return Created("", bid);
    }

    [Authorize(Roles = $"{nameof(UserRole.Auctioneer)},{nameof(UserRole.Fisherman)},{nameof(UserRole.BaitSeller)}")]
    [HttpPost("{id}/end")]
    public async Task<IActionResult> EndAuction(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var auction = await _auctionManager.EndAuctionAsync(id, userId);
        await _hubContext.Clients.Group($"auction-{id}").SendAsync("AuctionEnded", auction);
        return Ok(auction);
    }
}
