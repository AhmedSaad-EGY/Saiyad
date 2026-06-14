namespace Sayiad.Data.Repository.ReportRepo;

public interface IReportRepository
{
    Task<Report?> GetByIdAsync(int id);
    Task<PagedResult<Report>> GetAllAsync(ReportStatus? status, int page, int pageSize);
    Task<bool> UserReportedTodayAsync(int userId);
    Task AddAsync(Report report);
    Task UpdateAsync(Report report);
}
