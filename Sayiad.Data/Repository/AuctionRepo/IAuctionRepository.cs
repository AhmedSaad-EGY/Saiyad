using Sayiad.Data.Common;

namespace Sayiad.Data.Repository.AuctionRepo;

public interface IAuctionRepository
{
    Task<PagedResult<Auction>> GetActiveAsync(AuctionFilterRequest filter, PaginationRequest pagination);
    Task<Auction?> GetByIdAsync(int auctionId);
    Task<Auction?> GetByIdWithBidsAsync(int auctionId);
    Task<Auction?> GetByIdWithDetailsAsync(int auctionId);
    Task AddAsync(Auction auction);
    Task<List<Auction>> GetExpiredActiveAsync();
    Task<int> GetUserMonthlyAuctionCountAsync(int userId);
}
