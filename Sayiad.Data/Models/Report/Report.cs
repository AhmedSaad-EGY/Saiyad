namespace Sayiad.Data.Models;

public class Report
{
    public int Id { get; set; }
    public int ReporterId { get; set; }
    public ReportType Type { get; set; }
    public ReportTargetType TargetType { get; set; }
    public int? TargetId { get; set; }
    public string Message { get; set; } = string.Empty;
    public ReportStatus Status { get; set; } = ReportStatus.Pending;
    public string? AdminNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public User Reporter { get; set; } = null!;
}
