using Sayiad.Data.Data;

namespace Sayiad.Data.Repository.ReportRepo;

public class ReportRepository : IReportRepository
{
    private readonly ApplicationDbContext _db;

    public ReportRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Report?> GetByIdAsync(int reportId)
    {
        return await _db.Reports
            .Include(r => r.Reporter)
            .FirstOrDefaultAsync(r => r.Id == reportId);
    }

    public async Task<PagedResult<Report>> GetAllAsync(ReportStatus? status, int page, int pageSize)
    {
        var query = _db.Reports
            .Include(r => r.Reporter)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Report>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<bool> UserReportedTodayAsync(int userId)
    {
        return await _db.Reports
            .AnyAsync(r => r.ReporterId == userId
                        && r.CreatedAt.Date == DateTime.UtcNow.Date);
    }

    public async Task AddAsync(Report report)
    {
        _db.Reports.Add(report);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Report report)
    {
        _db.Reports.Update(report);
        await _db.SaveChangesAsync();
    }
}
