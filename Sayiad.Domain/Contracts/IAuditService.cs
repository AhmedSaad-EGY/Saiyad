namespace Sayiad.Domain.Contracts;

public interface IAuditService
{
    Task LogAsync(int? userId, string action, string entityType, int? entityId = null, string? oldValue = null, string? newValue = null);
}
