namespace Sayiad.Domain.Dtos.Subscription;

public record SubscriptionResponse(
    int Id,
    string Tier,
    decimal Price,
    int AuctionsPerMonth,
    int AuctionsUsedThisMonth,
    int AuctionsRemaining,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsActive,
    DateTime? RenewsAt
);
