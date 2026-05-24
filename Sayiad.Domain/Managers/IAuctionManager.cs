using Sayiad.Data.Common;
using Sayiad.Domain.Dtos.AuctionDtos;

namespace Sayiad.Domain.Managers;

public interface IAuctionManager
{
    Task<PagedResult<AuctionResponse>> GetActiveAsync(AuctionFilterRequest? filter = null, PaginationRequest? pagination = null);
    Task<AuctionDetailResponse> GetByIdAsync(int auctionId);
    Task<AuctionResponse> CreateAsync(int userId, CreateAuctionRequest request);
    Task<BidResponse> PlaceBidAsync(int auctionId, int userId, PlaceBidRequest request);
    Task<AuctionResponse> EndAuctionAsync(int auctionId, int userId);

    // Auction request system
    Task<AuctionRequestResponse> SubmitRequestAsync(int fishermanId, SubmitAuctionRequestRequest request);
    Task<PagedResult<AuctionRequestResponse>> GetMyRequestsAsync(int fishermanId, PaginationRequest pagination);
    Task<PagedResult<AuctionRequestResponse>> GetPendingRequestsAsync(PaginationRequest pagination);
    Task<AuctionResponse> ApproveRequestAsync(int auctionRequestId, int auctioneerId, ApproveAuctionRequestRequest request);
    Task<AuctionRequestResponse> RejectRequestAsync(int auctionRequestId, int auctioneerId, RejectAuctionRequestRequest request);

    // Auctioneer analytics
    Task<AuctioneerDashboardResponse> GetAuctioneerDashboardAsync(int auctioneerId);
}
