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
    Task<int> GetUserMonthlyBidCountAsync(int userId);
    Task<int> GetUserMonthlyRequestCountAsync(int userId);

    // Auction request system
    Task<AuctionRequest> CreateRequestAsync(AuctionRequest request);
    Task<AuctionRequest?> GetRequestByIdAsync(int id);
    Task<PagedResult<AuctionRequest>> GetPendingRequestsAsync(PaginationRequest pagination);
    Task<PagedResult<AuctionRequest>> GetFishermanRequestsAsync(int fishermanId, PaginationRequest pagination);
    Task<AuctionRequest> UpdateRequestAsync(AuctionRequest request);
    Task<List<Auction>> GetByCreatorAsync(int userId);
    Task<(int Pending, int Approved, int Rejected)> GetRequestCountsByStatusAsync();
    Task<(int Total, int Active, int Finished, int TotalBids, decimal TotalBidValue)> GetDashboardStatsAsync(int auctioneerId);
}
