
namespace Sayiad.Domain.Dtos.AuctionDtos;

/// <summary>Full auction details returned to clients.</summary>
public record AuctionResponse(int Id, int ProductId, string ProductTitle, string? ProductImageUrl, int? WinnerUserId, string? WinnerName, DateTime StartTime, DateTime EndTime, decimal StartingPrice, decimal ReservePrice, decimal BidIncrement, decimal CurrentHighestBid, AuctionStatus Status, int BidCount, DateTime CreatedAt);

/// <summary>Request to create a new auction for a product. StartTime is optional — null means start immediately.</summary>
public record CreateAuctionRequest(
    int ProductId,
    DateTime EndTime,
    decimal StartingPrice,
    decimal ReservePrice,
    decimal BidIncrement,
    DateTime? StartTime = null
);

/// <summary>Request to place a bid. Set MaxAutoBidAmount to enable automatic bidding up to that amount.</summary>
public record PlaceBidRequest(decimal Amount, decimal? MaxAutoBidAmount = null);

/// <summary>Response returned after a bid is placed.</summary>
public record BidResponse(int Id, int AuctionId, int UserId, string UserName, decimal Amount, bool IsAutoBid, decimal? MaxAutoBidAmount, string Status, DateTime CreatedAt);

/// <summary>Wraps an auction with its full bid history.</summary>
public record AuctionDetailResponse(AuctionResponse Auction, List<BidResponse> Bids);
