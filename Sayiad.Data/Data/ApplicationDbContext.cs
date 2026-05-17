namespace Sayiad.Data.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<ProductImage> ProductImages { get; set; } = null!;
        public DbSet<Auction> Auctions { get; set; } = null!;
        public DbSet<Bid> Bids { get; set; } = null!;
        public DbSet<CustomerOrder> CustomerOrders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<Transaction> Transactions { get; set; } = null!;
        public DbSet<ShippingAddress> ShippingAddresses { get; set; } = null!;
        public DbSet<Review> Reviews { get; set; } = null!;
        public DbSet<Cart> Carts { get; set; } = null!;
        public DbSet<CartItem> CartItems { get; set; } = null!;
        public DbSet<Wishlist> Wishlists { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<Report> Reports { get; set; } = null!;
        public DbSet<SellerProfile> SellerProfiles { get; set; } = null!;
        public DbSet<Subscription> Subscriptions { get; set; } = null!;
        public DbSet<AuctionRequest> AuctionRequests { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
            modelBuilder.ApplyConfiguration(new AuctionRequestConfiguration());

            // Fix decimal precision warnings
            modelBuilder.Entity<Bid>(b =>
            {
                b.Property(x => x.Amount).HasPrecision(18, 2);
                b.Property(x => x.MaxAutoBidAmount).HasPrecision(18, 2);
            });
            modelBuilder.Entity<OrderItem>(o =>
            {
                o.Property(x => x.UnitPrice).HasPrecision(18, 2);
                o.Property(x => x.Subtotal).HasPrecision(18, 2);
            });
            modelBuilder.Entity<Payment>(p =>
                p.Property(x => x.Amount).HasPrecision(18, 2));
            modelBuilder.Entity<SellerProfile>(s =>
                s.Property(x => x.AverageRating).HasPrecision(3, 2));
            modelBuilder.Entity<Transaction>(t =>
                t.Property(x => x.Amount).HasPrecision(18, 2));
        }
    }
}

