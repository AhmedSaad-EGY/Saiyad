using Sayiad.Data.Data;
using Sayiad.Data.Models;

namespace Sayiad.Data.Repository.SubscriptionRepo;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly ApplicationDbContext _db;

    public SubscriptionRepository(ApplicationDbContext db) => _db = db;

    public async Task<Subscription?> GetActiveAsync(int userId)
    {
        return await _db.Subscriptions
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Subscription>> GetUserSubscriptionsAsync(int userId)
    {
        return await _db.Subscriptions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.StartDate)
            .ToListAsync();
    }

    public async Task AddAsync(Subscription subscription)
    {
        _db.Subscriptions.Add(subscription);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Subscription subscription)
    {
        _db.Subscriptions.Update(subscription);
        await _db.SaveChangesAsync();
    }

    public async Task<PagedResult<Subscription>> GetAllAsync(PaginationRequest pagination)
    {
        var query = _db.Subscriptions
            .Include(s => s.User)
            .AsQueryable();

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(s => s.StartDate)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return new PagedResult<Subscription>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    public async Task<int> GetMonthlyAuctionCountAsync(int userId)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return await _db.Auctions
            .CountAsync(a => a.CreatedByUserId == userId && a.CreatedAt >= startOfMonth);
    }

    public async Task<Dictionary<int, int>> GetMonthlyAuctionCountsAsync(IEnumerable<int> userIds)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return await _db.Auctions
            .Where(a => userIds.Contains(a.CreatedByUserId) && a.CreatedAt >= startOfMonth)
            .GroupBy(a => a.CreatedByUserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.UserId, g => g.Count);
    }
}
