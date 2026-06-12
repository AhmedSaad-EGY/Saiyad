using Sayiad.Data.Common;
using Sayiad.Data.Repository.AuctionRepo;
using Sayiad.Data.Data;

namespace Sayiad.Data.Repository.AuctionRepo;

public class AuctionRepository : IAuctionRepository
{
    private readonly ApplicationDbContext _db;

    public AuctionRepository(ApplicationDbContext db) => _db = db;

    public async Task<PagedResult<Auction>> GetActiveAsync(AuctionFilterRequest filter, PaginationRequest pagination)
    {
        var query = _db.Auctions
            .Include(a => a.Product!).ThenInclude(p => p.Images)
            .Include(a => a.Bids)
            .Where(a => a.Product != null);

        if (string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(a => a.Status == AuctionStatus.Active);
        else if (Enum.TryParse<AuctionStatus>(filter.Status, ignoreCase: true, out var statusFilter))
            query = query.Where(a => a.Status == statusFilter);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            query = query.Where(a =>
                a.Product!.Title.Contains(filter.SearchTerm) ||
                a.Product!.Description.Contains(filter.SearchTerm));
        if (filter.MinPrice.HasValue)
            query = query.Where(a => a.CurrentHighestBid >= filter.MinPrice);
        if (filter.MaxPrice.HasValue)
            query = query.Where(a => a.CurrentHighestBid <= filter.MaxPrice);

        if (filter.EndingSoon.HasValue && filter.EndingSoon.Value)
            query = query.Where(a => a.EndTime <= DateTime.UtcNow.AddHours(24));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return new PagedResult<Auction>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    public async Task<Auction?> GetByIdAsync(int auctionId)
    {
        return await _db.Auctions.FindAsync(auctionId);
    }

    public async Task<Auction?> GetByIdWithBidsAsync(int auctionId)
    {
        return await _db.Auctions
            .Include(a => a.Bids)
            .FirstOrDefaultAsync(a => a.Id == auctionId);
    }

    public async Task<Auction?> GetByIdWithDetailsAsync(int auctionId)
    {
        return await _db.Auctions
            .Include(a => a.Product!).ThenInclude(p => p.Images)
            .Include(a => a.Bids).ThenInclude(b => b.User)
            .Include(a => a.Winner)
            .FirstOrDefaultAsync(a => a.Id == auctionId);
    }

    public async Task AddAsync(Auction auction)
    {
        _db.Auctions.Add(auction);
        await _db.SaveChangesAsync();
    }

    public async Task<int> GetUserMonthlyAuctionCountAsync(int userId)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return await _db.Auctions
            .CountAsync(a => a.CreatedByUserId == userId && a.CreatedAt >= startOfMonth);
    }

    public async Task<int> GetUserMonthlyBidCountAsync(int userId)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return await _db.Bids
            .CountAsync(b => b.UserId == userId && b.CreatedAt >= startOfMonth
                          && !b.IsAutoBid);
    }

    public async Task<int> GetUserMonthlyRequestCountAsync(int userId)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return await _db.AuctionRequests
            .CountAsync(r => r.FishermanId == userId && r.CreatedAt >= startOfMonth);
    }

    public async Task<List<Auction>> GetExpiredActiveAsync()
    {
        return await _db.Auctions
            .Where(a => a.Status == AuctionStatus.Active
                     && a.EndTime <= DateTime.UtcNow)
            .Include(a => a.Bids)
            .Include(a => a.Product)
            .Include(a => a.Winner)
            .ToListAsync();
    }

    public async Task<AuctionRequest> CreateRequestAsync(AuctionRequest request)
    {
        await _db.AuctionRequests.AddAsync(request);
        await _db.SaveChangesAsync();
        return request;
    }

    public async Task<AuctionRequest?> GetRequestByIdAsync(int id)
        => await _db.AuctionRequests
            .Include(r => r.Fisherman)
            .Include(r => r.ReviewedByAuctioneer)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<PagedResult<AuctionRequest>> GetPendingRequestsAsync(PaginationRequest pagination)
    {
        var query = _db.AuctionRequests
            .Include(r => r.Fisherman)
            .Where(r => r.Status == AuctionRequestStatus.Pending)
            .OrderByDescending(r => r.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return new PagedResult<AuctionRequest>
        {
            Items = items,
            TotalCount = total,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    public async Task<PagedResult<AuctionRequest>> GetFishermanRequestsAsync(int fishermanId, PaginationRequest pagination)
    {
        var query = _db.AuctionRequests
            .Where(r => r.FishermanId == fishermanId)
            .OrderByDescending(r => r.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return new PagedResult<AuctionRequest>
        {
            Items = items,
            TotalCount = total,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    public async Task<AuctionRequest> UpdateRequestAsync(AuctionRequest request)
    {
        request.UpdatedAt = DateTime.UtcNow;
        _db.AuctionRequests.Update(request);
        await _db.SaveChangesAsync();
        return request;
    }

    public async Task<List<Auction>> GetByCreatorAsync(int userId)
        => await _db.Auctions
            .Include(a => a.Bids)
            .Where(a => a.CreatedByUserId == userId)
            .ToListAsync();

    public async Task<(int Pending, int Approved, int Rejected)> GetRequestCountsByStatusAsync()
    {
        var counts = await _db.AuctionRequests
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return (
            counts.FirstOrDefault(c => c.Status == AuctionRequestStatus.Pending)?.Count ?? 0,
            counts.FirstOrDefault(c => c.Status == AuctionRequestStatus.Approved)?.Count ?? 0,
            counts.FirstOrDefault(c => c.Status == AuctionRequestStatus.Rejected)?.Count ?? 0
        );
    }

    public async Task<(int Total, int Active, int Finished, int TotalBids, decimal TotalBidValue)> GetDashboardStatsAsync(int auctioneerId)
    {
        var auctions = _db.Auctions.Where(a => a.CreatedByUserId == auctioneerId);
        var total = await auctions.CountAsync();
        var active = await auctions.CountAsync(a => a.Status == AuctionStatus.Active);
        var finished = await auctions.CountAsync(a => a.Status == AuctionStatus.Finished);

        var bidStats = await auctions
            .SelectMany(a => a.Bids)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalBids = g.Count(),
                TotalBidValue = g.Sum(b => b.Amount)
            })
            .FirstOrDefaultAsync();

        return (
            total,
            active,
            finished,
            bidStats?.TotalBids ?? 0,
            bidStats?.TotalBidValue ?? 0
        );
    }
}
