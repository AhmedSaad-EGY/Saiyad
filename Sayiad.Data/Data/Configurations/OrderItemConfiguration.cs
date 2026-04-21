namespace Sayiad.Api.Data.Configurations
{
    public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.HasKey(oi => oi.Id);
            builder.Property(oi => oi.UnitPrice).HasPrecision(18, 2);
            builder.Property(oi => oi.Subtotal).HasPrecision(18, 2);
            builder.Property(oi => oi.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(oi => oi.Product)
                   .WithMany(p => p.OrderItems)
                   .HasForeignKey(oi => oi.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(oi => oi.Seller)
                   .WithMany(u => u.SoldOrderItems)
                   .HasForeignKey(oi => oi.SellerId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
