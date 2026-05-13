using Sayiad.Domain.Dtos.NotificationDtos;

namespace Sayiad.Domain.Managers;

public interface INotificationManager
{
    Task<IEnumerable<NotificationResponse>> GetUserNotificationsAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
    Task MarkAsReadAsync(int notificationId, int userId);
    Task MarkAllAsReadAsync(int userId);
    Task CreateAsync(int userId, string title, string message);
}
