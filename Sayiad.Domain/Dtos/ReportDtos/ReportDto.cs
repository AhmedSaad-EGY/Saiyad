namespace Sayiad.Domain.Dtos.ReportDtos;

public record ReportResponse(int Id, int ReporterId, string ReporterName, int ProductId, string ProductTitle, string Reason, string Status, DateTime CreatedAt);
public record CreateReportRequest(int ProductId, string Reason);
public record ResolveReportRequest(string Status);
