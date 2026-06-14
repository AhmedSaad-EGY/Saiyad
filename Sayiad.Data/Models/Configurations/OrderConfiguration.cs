
namespace Sayiad.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("CustomerOrders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.TotalPrice).HasPrecision(18, 2);
        builder.Property(o => o.OrderType)
            .HasMaxLength(20)
            .HasConversion<string>();
        builder.Property(o => o.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();
        builder.Property(o => o.ReturnReason).HasMaxLength(1000);
        builder.Property(o => o.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(o => o.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(o => o.ShippingAddress)
               .WithMany()
               .HasForeignKey(o => o.ShippingAddressId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.OrderItems)
               .WithOne(oi => oi.Order)
               .HasForeignKey(oi => oi.OrderId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.Payments)
               .WithOne(p => p.Order)
               .HasForeignKey(p => p.OrderId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
