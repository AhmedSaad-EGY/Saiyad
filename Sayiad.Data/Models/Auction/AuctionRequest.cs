namespace Sayiad.Data.Models;

public class AuctionRequest
{
    public int Id { get; set; }
    public int FishermanId { get; set; }
    public int? ReviewedByAuctioneerId { get; set; }
    public int? ResultingAuctionId { get; set; }

    public string ProductTitle { get; set; } = null!;
    public string ProductDescription { get; set; } = null!;
    public string? ProductImageUrl { get; set; }
    public decimal EstimatedValue { get; set; }
    public decimal QuantityKg { get; set; }
    public string FishType { get; set; } = null!;
    public string CatchLocation { get; set; } = null!;
    public DateTime CatchDate { get; set; }

    public AuctionRequestStatus Status { get; set; } = AuctionRequestStatus.Pending;
    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? Fisherman { get; set; }
    public User? ReviewedByAuctioneer { get; set; }
    public Auction? ResultingAuction { get; set; }
}
