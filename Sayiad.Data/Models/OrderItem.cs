namespace Sayiad.Data.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int SellerId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
        public DateTime CreatedAt { get; set; }

        public CustomerOrder Order { get; set; } = null!;
        public Product Product { get; set; } = null!;
        public User Seller { get; set; } = null!;
    }
}
