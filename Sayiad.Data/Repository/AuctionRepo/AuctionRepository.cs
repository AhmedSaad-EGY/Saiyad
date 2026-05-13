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
            .Include(a => a.Product).ThenInclude(p => p.Images)
            .Include(a => a.Bids)
            .Where(a => a.Status == AuctionStatus.Active);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            query = query.Where(a =>
                a.Product != null && (
                    a.Product.Title.Contains(filter.SearchTerm) ||
                    a.Product.Description.Contains(filter.SearchTerm)));
        if (filter.MinPrice.HasValue)
            query = query.Where(a => a.CurrentHighestBid >= filter.MinPrice);
        if (filter.MaxPrice.HasValue)
            query = query.Where(a => a.CurrentHighestBid <= filter.MaxPrice);

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
            .Include(a => a.Product).ThenInclude(p => p.Images)
            .Include(a => a.Bids).ThenInclude(b => b.User)
            .Include(a => a.Winner)
            .FirstOrDefaultAsync(a => a.Id == auctionId);
    }

    public async Task AddAsync(Auction auction)
    {
        _db.Auctions.Add(auction);
        await _db.SaveChangesAsync();
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _db.SaveChangesAsync();
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
}
