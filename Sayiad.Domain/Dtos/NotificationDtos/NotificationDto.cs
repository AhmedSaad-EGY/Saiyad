namespace Sayiad.Domain.Dtos.NotificationDtos;

public record NotificationResponse(int Id, string Title, string Message, bool IsRead, DateTime CreatedAt);
