namespace Sayiad.Data.Models
{
    public class Product
    {
        public int Id { get; set; }
        public int SellerId { get; set; }
        public int CategoryId { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Brand { get; set; } = null!;
        public ProductCondition Condition { get; set; } // New, Used
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string Location { get; set; } = null!;
        public bool IsAuctioned { get; set; }
        public ProductStatus Status { get; set; } // Available, Sold, Draft
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public User Seller { get; set; } = null!;
        public Category Category { get; set; } = null!;
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<Auction> Auctions { get; set; } = new List<Auction>();
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
        public ICollection<Report> Reports { get; set; } = new List<Report>();
    }

}
