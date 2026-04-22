namespace Sayiad.Data.Models
{
    public class SellerProfile
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string StoreName { get; set; } = null!;
        public string? StoreDescription { get; set; }
        public decimal AverageRating { get; set; }
        public int TotalSales { get; set; }
        public DateTime CreatedAt { get; set; }

        public User User { get; set; } = null!;
    }
}
