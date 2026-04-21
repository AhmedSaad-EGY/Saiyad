using System.Net.NetworkInformation;

namespace Sayiad.Api.Models
{
    public class Bid
    {
        public int Id { get; set; }
        public int AuctionId { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public bool IsAutoBid { get; set; }
        public BidStatus BidStatus { get; set; } // Valid, Rejected, Winning
        public DateTime CreatedAt { get; set; }

        public Auction Auction { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
