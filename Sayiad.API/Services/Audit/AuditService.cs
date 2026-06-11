using Sayiad.Data.Data;
using Sayiad.Data.Models;
using Sayiad.Domain.Contracts;

namespace Sayiad.Api.Services.Audit;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;

    public AuditService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(int? userId, string action, string entityType, int? entityId = null, string? oldValue = null, string? newValue = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValue = oldValue,
            NewValue = newValue,
            Timestamp = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
