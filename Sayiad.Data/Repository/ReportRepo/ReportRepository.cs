using Sayiad.Data.Data;
using Sayiad.Data.Repository.ReportRepo;

namespace Sayiad.Data.Repository.ReportRepo;

public class ReportRepository : IReportRepository
{
    private readonly ApplicationDbContext _db;

    public ReportRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Report>> GetAllAsync()
    {
        return await _db.Reports
            .Include(r => r.Reporter)
            .Include(r => r.Product)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Report?> GetByIdAsync(int reportId)
    {
        return await _db.Reports
            .Include(r => r.Reporter)
            .Include(r => r.Product)
            .FirstOrDefaultAsync(r => r.Id == reportId);
    }

    public async Task<bool> ExistsByReporterAndProductAsync(int reporterId, int productId)
    {
        return await _db.Reports
            .AnyAsync(r => r.ReporterId == reporterId && r.ProductId == productId && r.Status == "Pending");
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
