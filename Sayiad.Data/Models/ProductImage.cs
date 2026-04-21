namespace Sayiad.Api.Models
{
    public class ProductImage
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ImageUrl { get; set; } = null!;
        public bool IsPrimary { get; set; }
        public DateTime CreatedAt { get; set; }

        public Product Product { get; set; } = null!;
    }
}
