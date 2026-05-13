
namespace Sayiad.Domain.Dtos.AuctionDtos;

public record AuctionResponse(int Id, int ProductId, string ProductTitle, string? ProductImageUrl, int? WinnerUserId, string? WinnerName, DateTime StartTime, DateTime EndTime, decimal StartingPrice, decimal ReservePrice, decimal MinimumIncrement, decimal CurrentHighestBid, AuctionStatus Status, int BidCount, DateTime CreatedAt);
public record CreateAuctionRequest(int ProductId, DateTime EndTime, decimal StartingPrice, decimal ReservePrice, decimal MinimumIncrement);
public record PlaceBidRequest(decimal Amount);
public record BidResponse(int Id, int UserId, string UserName, decimal Amount, bool IsAutoBid, string Status, DateTime CreatedAt);
public record AuctionDetailResponse(AuctionResponse Auction, List<BidResponse> Bids);
