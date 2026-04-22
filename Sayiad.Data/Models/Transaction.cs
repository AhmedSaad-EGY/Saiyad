namespace Sayiad.Data.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public int PaymentId { get; set; }
        public string TransactionReference { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public Payment Payment { get; set; } = null!;
    }
}
