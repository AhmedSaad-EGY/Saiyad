namespace Sayiad.Data.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int UserId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }

        public Product Product { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
