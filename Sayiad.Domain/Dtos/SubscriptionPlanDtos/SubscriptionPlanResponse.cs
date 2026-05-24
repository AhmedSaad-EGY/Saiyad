namespace Sayiad.Domain.Dtos.SubscriptionPlanDtos;

public record SubscriptionPlanResponse(
    int Id, string Tier, string Name, string? Description,
    decimal Price, string Currency, string BillingCycle,
    int MaxAuctionsPerMonth, int MaxBidsPerMonth, int MaxAuctionRequestsPerMonth,
    string[] Features, bool IsActive, int SortOrder
);

public record CreateSubscriptionPlanRequest(
    string Tier, string Name, string? Description,
    decimal Price, string Currency, string BillingCycle,
    int MaxAuctionsPerMonth, int MaxBidsPerMonth, int MaxAuctionRequestsPerMonth,
    string[] Features, int SortOrder
);

public record UpdateSubscriptionPlanRequest(
    string? Name, string? Description,
    decimal? Price, string? Currency, string? BillingCycle,
    int? MaxAuctionsPerMonth, int? MaxBidsPerMonth, int? MaxAuctionRequestsPerMonth,
    string[]? Features, bool? IsActive, int? SortOrder
);
