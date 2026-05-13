using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sayiad.Domain.Dtos.SellerProfileDtos;

namespace Sayiad.Api.Controllers;

[ApiController]
[Route("api/seller-profile")]
public class SellerProfileController : ControllerBase
{
    private readonly ISellerProfileManager _sellerProfileManager;

    public SellerProfileController(ISellerProfileManager sellerProfileManager)
    {
        _sellerProfileManager = sellerProfileManager;
    }

    [HttpPost]
    [Authorize(Roles = $"{nameof(UserRole.Fisherman)},{nameof(UserRole.BaitSeller)}")]
    public async Task<IActionResult> Create(CreateSellerProfileRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var profile = await _sellerProfileManager.CreateAsync(userId, request);
        return StatusCode(201, profile);
    }

    [HttpGet("{userId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByUserId(int userId)
    {
        var profile = await _sellerProfileManager.GetByUserIdAsync(userId);
        return Ok(profile);
    }

    [HttpPut]
    [Authorize(Roles = $"{nameof(UserRole.Fisherman)},{nameof(UserRole.BaitSeller)}")]
    public async Task<IActionResult> Update(UpdateSellerProfileRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var profile = await _sellerProfileManager.UpdateAsync(userId, request);
        return Ok(profile);
    }

    [HttpGet("me")]
    [Authorize(Roles = $"{nameof(UserRole.Fisherman)},{nameof(UserRole.BaitSeller)}")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var profile = await _sellerProfileManager.GetMyProfileAsync(userId);
        return Ok(profile);
    }

    [HttpGet("dashboard")]
    [Authorize(Roles = $"{nameof(UserRole.Fisherman)},{nameof(UserRole.BaitSeller)}")]
    public async Task<IActionResult> GetDashboard()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var dashboard = await _sellerProfileManager.GetDashboardAsync(userId);
        return Ok(dashboard);
    }
}
