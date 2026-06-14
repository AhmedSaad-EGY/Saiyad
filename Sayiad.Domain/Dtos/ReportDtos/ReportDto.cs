namespace Sayiad.Domain.Dtos.ReportDtos;

public record SubmitReportRequest(
    ReportType Type,
    ReportTargetType TargetType,
    int? TargetId,
    string Message);

public record ReportResponse(
    int Id,
    ReportType Type,
    ReportTargetType TargetType,
    int? TargetId,
    string Message,
    ReportStatus Status,
    string? AdminNote,
    DateTime CreatedAt,
    DateTime? ResolvedAt);

public record ResolveReportRequest(
    ReportStatus NewStatus,
    string? AdminNote);
