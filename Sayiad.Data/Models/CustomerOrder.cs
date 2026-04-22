namespace Sayiad.Data.Models
{
    public class CustomerOrder
    {
        public int Id { get; set; }
        public int BuyerId { get; set; }
        public decimal TotalPrice { get; set; }
        public CustomerOrderStatus Status { get; set; } // Pending, Paid, Shipped, Delivered
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public User Buyer { get; set; } = null!;
        public ICollection<OrderItem> OrderItems { get; set; } = new HashSet<OrderItem>();
        public ICollection<Payment> Payments { get; set; } = new HashSet<Payment>();
    }
}
