namespace Sayiad.Domain.Dtos.AuctionDtos;

/// <summary>Fisherman submits this to request an auction.</summary>
public record SubmitAuctionRequestRequest(
    string ProductTitle,
    string ProductDescription,
    string? ProductImageUrl,
    decimal EstimatedValue,
    decimal QuantityKg,
    string FishType,
    string CatchLocation,
    DateTime? CatchDate
);

/// <summary>Auctioneer approves and sets auction parameters.</summary>
public record ApproveAuctionRequestRequest(
    DateTime EndTime,
    decimal StartingPrice,
    decimal ReservePrice,
    decimal MinimumIncrement
);

/// <summary>Auctioneer rejects with a reason.</summary>
public record RejectAuctionRequestRequest(
    string Reason
);

/// <summary>Response returned for auction requests.</summary>
public record AuctionRequestResponse(
    int Id,
    int FishermanId,
    string FishermanName,
    string ProductTitle,
    string ProductDescription,
    string? ProductImageUrl,
    decimal EstimatedValue,
    decimal QuantityKg,
    string FishType,
    string CatchLocation,
    DateTime CatchDate,
    string Status,
    string? RejectionReason,
    int? ResultingAuctionId,
    DateTime CreatedAt
);
