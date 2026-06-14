namespace Sayiad.Data.Models;
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string? ProfileImage { get; set; }
        public UserRole Role { get; set; }
        public UserRole? RequestedRole { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsEmailVerified { get; set; }
        public string? EmailVerificationToken { get; set; }
        public string? RefreshToken { get; set; }
        public string? PreviousRefreshTokenHash { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiry { get; set; }
        public string? LicenseNumber { get; set; }
        public DateTime? Birthdate { get; set; }
        public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.Free;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Relations
        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
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
        public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
        public Wallet? Wallet { get; set; }
    }
