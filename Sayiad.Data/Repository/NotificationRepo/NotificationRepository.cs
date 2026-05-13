using Sayiad.Data.Data;
using Sayiad.Data.Repository.NotificationRepo;

namespace Sayiad.Data.Repository.NotificationRepo;

public class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _db;

    public NotificationRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId)
    {
        return await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _db.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task<Notification?> GetByIdAsync(int notificationId, int userId)
    {
        return await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
    }

    public async Task MarkAsReadAsync(Notification notification)
    {
        notification.IsRead = true;
        await _db.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true));
    }

    public async Task AddAsync(Notification notification)
    {
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();
    }
}
