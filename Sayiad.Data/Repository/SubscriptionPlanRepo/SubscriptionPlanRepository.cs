using Sayiad.Data.Data;

namespace Sayiad.Data.Repository.SubscriptionPlanRepo;

public class SubscriptionPlanRepository : ISubscriptionPlanRepository
{
    private readonly ApplicationDbContext _db;

    public SubscriptionPlanRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<SubscriptionPlan>> GetActivePlansAsync()
    {
        return await _db.Set<SubscriptionPlan>()
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();
    }

    public async Task<SubscriptionPlan?> GetByIdAsync(int id)
    {
        return await _db.Set<SubscriptionPlan>().FindAsync(id);
    }

    public async Task<SubscriptionPlan?> GetByTierAsync(SubscriptionTier tier)
    {
        return await _db.Set<SubscriptionPlan>()
            .FirstOrDefaultAsync(p => p.Tier == tier);
    }

    public async Task<SubscriptionPlan> CreateAsync(SubscriptionPlan plan)
    {
        _db.Set<SubscriptionPlan>().Add(plan);
        await _db.SaveChangesAsync();
        return plan;
    }

    public async Task<SubscriptionPlan> UpdateAsync(SubscriptionPlan plan)
    {
        _db.Set<SubscriptionPlan>().Update(plan);
        await _db.SaveChangesAsync();
        return plan;
    }

    public async Task DeleteAsync(SubscriptionPlan plan)
    {
        _db.Set<SubscriptionPlan>().Remove(plan);
        await _db.SaveChangesAsync();
    }

    public async Task<PagedResult<SubscriptionPlan>> GetAllAsync(PaginationRequest pagination)
    {
        var query = _db.Set<SubscriptionPlan>().AsQueryable();
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(p => p.SortOrder)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return new PagedResult<SubscriptionPlan>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }
}
