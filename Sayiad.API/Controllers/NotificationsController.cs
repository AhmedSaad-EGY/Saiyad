using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sayiad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationManager _notificationManager;

    public NotificationsController(INotificationManager notificationManager)
    {
        _notificationManager = notificationManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var notifications = await _notificationManager.GetUserNotificationsAsync(userId);
        return Ok(notifications);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var count = await _notificationManager.GetUnreadCountAsync(userId);
        return Ok(new { count });
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _notificationManager.MarkAsReadAsync(id, userId);
        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _notificationManager.MarkAllAsReadAsync(userId);
        return NoContent();
    }
}
