namespace Sayiad.Data.Models;

public class SubscriptionPlan
{
    public int Id { get; set; }
    public SubscriptionTier Tier { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public string BillingCycle { get; set; } = "Monthly";
    public int MaxAuctionsPerMonth { get; set; }
    public int MaxBidsPerMonth { get; set; }
    public int MaxAuctionRequestsPerMonth { get; set; }
    public string Features { get; set; } = "[]";
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}
