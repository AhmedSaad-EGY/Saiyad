using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sayiad.Domain.Dtos.UserDtos;

namespace Sayiad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserManager _userManager;

    public UsersController(IUserManager userManager)
    {
        _userManager = userManager;
    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _userManager.GetProfileAsync(userId);
        return Ok(result);
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(UpdateUserRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _userManager.UpdateProfileAsync(userId, request);
        return Ok(result);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationRequest? pagination)
    {
        var users = await _userManager.GetAllUsersAsync(pagination);
        return Ok(users);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userManager.GetUserByIdAsync(id);
        return Ok(user);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPatch("{id}/toggle-status")]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        await _userManager.ToggleUserStatusAsync(id);
        return NoContent();
    }

    [Authorize]
    [HttpGet("~/api/user")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _userManager.GetProfileAsync(userId);
        return Ok(result);
    }
}
