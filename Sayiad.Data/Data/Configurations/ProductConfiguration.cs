namespace Sayiad.Api.Data.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Title).IsRequired().HasMaxLength(200);
            builder.Property(p => p.Description).IsRequired();
            builder.Property(p => p.Brand).IsRequired().HasMaxLength(50);
            builder.Property(p => p.Condition).IsRequired().HasMaxLength(20);
            builder.Property(p => p.Price).IsRequired().HasPrecision(18, 2);
            builder.Property(p => p.StockQuantity).IsRequired();
            builder.Property(p => p.Location).IsRequired().HasMaxLength(100);
            builder.Property(p => p.Status).IsRequired().HasMaxLength(20);
            builder.Property(p => p.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            builder.Property(p => p.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

            builder.HasMany(p => p.Images)
                   .WithOne(pi => pi.Product)
                   .HasForeignKey(pi => pi.ProductId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.Auctions)
                   .WithOne(a => a.Product)
                   .HasForeignKey(a => a.ProductId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.OrderItems)
                   .WithOne(oi => oi.Product)
                   .HasForeignKey(oi => oi.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(p => p.Reviews)
                   .WithOne(r => r.Product)
                   .HasForeignKey(r => r.ProductId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.CartItems)
                   .WithOne(ci => ci.Product)
                   .HasForeignKey(ci => ci.ProductId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.Wishlists)
                   .WithOne(w => w.Product)
                   .HasForeignKey(w => w.ProductId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.Reports)
                   .WithOne(r => r.Product)
                   .HasForeignKey(r => r.ProductId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
