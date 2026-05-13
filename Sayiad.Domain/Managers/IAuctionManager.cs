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
}
