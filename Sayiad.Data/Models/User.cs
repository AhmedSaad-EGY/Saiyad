namespace Sayiad.Api.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string? ProfileImage { get; set; }
        public UserRole Role { get; set; } //enum: Buyer, Seller, Admin
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Relations
        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<CustomerOrder> CustomerOrders { get; set; } = new List<CustomerOrder>();
        public ICollection<Bid> Bids { get; set; } = new List<Bid>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<ShippingAddress> ShippingAddresses { get; set; } = new List<ShippingAddress>();
        public Cart? Cart { get; set; }
        public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<Report> Reports { get; set; } = new List<Report>();
        public SellerProfile? SellerProfile { get; set; }
        public ICollection<Auction> WonAuctions { get; set; } = new List<Auction>();
        public ICollection<OrderItem> SoldOrderItems { get; set; } = new List<OrderItem>();
    }
}
