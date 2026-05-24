using Microsoft.Extensions.Logging;
using Sayiad.Domain.Dtos.NotificationDtos;

namespace Sayiad.Domain.Managers;

public class NotificationManager : INotificationManager
{
    private readonly INotificationRepository _repo;
    private readonly ILogger<NotificationManager> _logger;

    public NotificationManager(INotificationRepository repo, ILogger<NotificationManager> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<IEnumerable<NotificationResponse>> GetUserNotificationsAsync(int userId)
    {
        var notifications = await _repo.GetUserNotificationsAsync(userId);
        return notifications.Select(n => new NotificationResponse(
            n.Id, n.Title, n.Message, n.IsRead, n.CreatedAt));
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _repo.GetUnreadCountAsync(userId);
    }

    public async Task MarkAsReadAsync(int notificationId, int userId)
    {
        var notification = await _repo.GetByIdAsync(notificationId, userId)
            ?? throw new KeyNotFoundException("Notification not found");
        await _repo.MarkAsReadAsync(notification);
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        await _repo.MarkAllAsReadAsync(userId);
        _logger.LogInformation("All notifications marked as read for user {UserId}", userId);
    }

    public async Task CreateAsync(int userId, string title, string message)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(notification);
        _logger.LogInformation("Notification created for user {UserId}: {Title}", userId, title);
    }
}