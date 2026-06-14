using Microsoft.Extensions.Logging;
using Sayiad.Domain.Dtos.ReportDtos;

namespace Sayiad.Domain.Managers;

public class ReportManager : IReportManager
{
    private readonly IReportRepository _repo;
    private readonly ILogger<ReportManager> _logger;
    private readonly INotificationManager _notificationManager;
    private readonly IUserRepository _userRepo;

    public ReportManager(
        IReportRepository repo,
        ILogger<ReportManager> logger,
        INotificationManager notificationManager,
        IUserRepository userRepo)
    {
        _repo = repo;
        _logger = logger;
        _notificationManager = notificationManager;
        _userRepo = userRepo;
    }

    public async Task<ReportResponse> SubmitReportAsync(int reporterId, SubmitReportRequest request)
    {
        if (await _repo.UserReportedTodayAsync(reporterId))
            throw new InvalidOperationException(
                "You can only submit one report per day. Please try again tomorrow.");

        var report = new Report
        {
            ReporterId = reporterId,
            Type = request.Type,
            TargetType = request.TargetType,
            TargetId = request.TargetId,
            Message = request.Message,
            Status = ReportStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(report);

        _logger.LogInformation("Report created: {ReportId}, type {Type}, target {TargetType}#{TargetId}",
            report.Id, request.Type, request.TargetType, request.TargetId);

        // Notify all admins about new report
        var admins = await _userRepo.GetUsersByRoleAsync(UserRole.Admin);
        foreach (var admin in admins)
        {
            await _notificationManager.CreateAsync(admin.Id, "New Report",
                $"A new {request.Type} report has been filed (Report #{report.Id}).");
        }

        return MapToResponse(report);
    }

    public async Task<PagedResult<ReportResponse>> GetAllAsync(ReportStatus? status, int page, int pageSize)
    {
        var result = await _repo.GetAllAsync(status, page, pageSize);
        return new PagedResult<ReportResponse>
        {
            Items = result.Items.Select(MapToResponse).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }

    public async Task<ReportResponse> ResolveAsync(int reportId, ResolveReportRequest request)
    {
        var report = await _repo.GetByIdAsync(reportId)
            ?? throw new KeyNotFoundException("Report not found");

        if (request.NewStatus != ReportStatus.Resolved && request.NewStatus != ReportStatus.Dismissed)
            throw new InvalidOperationException("Report can only be resolved or dismissed.");

        report.Status = request.NewStatus;
        report.AdminNote = request.AdminNote;
        report.ResolvedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(report);

        await _notificationManager.CreateAsync(report.ReporterId, "Report Update",
            $"Your report #{reportId} has been {request.NewStatus}. " +
            (request.NewStatus == ReportStatus.Resolved
                ? "Thank you for helping keep the platform safe."
                : ""));

        _logger.LogInformation("Report {ReportId} resolved to {Status}", reportId, request.NewStatus);
        return MapToResponse(report);
    }

    private static ReportResponse MapToResponse(Report report) => new(
        report.Id, report.Type, report.TargetType, report.TargetId,
        report.Message, report.Status, report.AdminNote,
        report.CreatedAt, report.ResolvedAt);
}
