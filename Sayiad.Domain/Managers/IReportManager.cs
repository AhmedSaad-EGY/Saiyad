using Sayiad.Domain.Dtos.ReportDtos;

namespace Sayiad.Domain.Managers;

public interface IReportManager
{
    Task<ReportResponse> SubmitReportAsync(int reporterId, SubmitReportRequest request);
    Task<PagedResult<ReportResponse>> GetAllAsync(ReportStatus? status, int page, int pageSize);
    Task<ReportResponse> ResolveAsync(int reportId, ResolveReportRequest request);
}
