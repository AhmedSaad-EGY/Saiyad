namespace Sayiad.Data.Models;

public class Order
{
    public int Id { get; set; }
    public int BuyerId { get; set; }
    public int ShippingAddressId { get; set; }
    public decimal TotalPrice { get; set; }
    public OrderType OrderType { get; set; } = OrderType.Product;
    public OrderStatus Status { get; set; }
    public int? AuctionId { get; set; }
    public bool ReturnRequested { get; set; }
    public DateTime? ReturnRequestedAt { get; set; }
    public string? ReturnReason { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User Buyer { get; set; } = null!;
    public ShippingAddress ShippingAddress { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = new HashSet<OrderItem>();
    public ICollection<Payment> Payments { get; set; } = new HashSet<Payment>();
}
