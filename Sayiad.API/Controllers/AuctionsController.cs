using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sayiad.Domain.Dtos.AuctionDtos;

namespace Sayiad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuctionsController : ControllerBase
{
    private readonly IAuctionManager _auctionManager;

    public AuctionsController(IAuctionManager auctionManager)
    {
        _auctionManager = auctionManager;
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

    [Authorize(Roles = $"{nameof(UserRole.Customer)},{nameof(UserRole.Fisherman)},{nameof(UserRole.BaitSeller)}")]
    [HttpPost("{id}/bids")]
    public async Task<IActionResult> PlaceBid(int id, PlaceBidRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var bid = await _auctionManager.PlaceBidAsync(id, userId, request);
        return Created("", bid);
    }

    [Authorize(Roles = $"{nameof(UserRole.Auctioneer)},{nameof(UserRole.Fisherman)},{nameof(UserRole.BaitSeller)}")]
    [HttpPost("{id}/end")]
    public async Task<IActionResult> EndAuction(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var auction = await _auctionManager.EndAuctionAsync(id, userId);
        return Ok(auction);
    }
}
