namespace Sayiad.Data.Repository.NotificationRepo;

public interface INotificationRepository
{
    Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
    Task<Notification?> GetByIdAsync(int notificationId, int userId);
    Task MarkAsReadAsync(Notification notification);
    Task MarkAllAsReadAsync(int userId);
    Task AddAsync(Notification notification);
}
