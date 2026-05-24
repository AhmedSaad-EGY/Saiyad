namespace Sayiad.Domain.Dtos.AuctionDtos;

public record AuctioneerDashboardResponse(
    int TotalAuctions,
    int ActiveAuctions,
    int FinishedAuctions,
    int PendingRequests,
    int ApprovedRequests,
    int RejectedRequests,
    decimal TotalBidValue,
    int TotalBids,
    double AverageBidsPerAuction
);
