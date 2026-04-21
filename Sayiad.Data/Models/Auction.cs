namespace Sayiad.Api.Models
{
    public class Auction
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int? WinnerUserId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal StartingPrice { get; set; }
        public decimal ReservePrice { get; set; }
        public decimal MinimumIncrement { get; set; }
        public decimal CurrentHighestBid { get; set; }
        public AuctionStatus Status { get; set; } 
        public DateTime CreatedAt { get; set; }

        public Product? Product { get; set; }
        public User? Winner { get; set; }
        public ICollection<Bid> Bids { get; set; } = new HashSet<Bid>();
    }
}
