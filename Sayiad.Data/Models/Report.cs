namespace Sayiad.Data.Models
{
    public class Report
    {
        public int Id { get; set; }
        public int ReporterId { get; set; }
        public int ProductId { get; set; }
        public string Reason { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public User Reporter { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
