namespace Sayiad.Data.Data.Configurations
{
    public class CustomerOrderConfiguration : IEntityTypeConfiguration<CustomerOrder>
    {
        public void Configure(EntityTypeBuilder<CustomerOrder> builder)
        {
            builder.HasKey(o => o.Id);
            builder.Property(o => o.TotalPrice).HasPrecision(18, 2);
            builder.Property(o => o.Status).IsRequired().HasMaxLength(20);
            builder.Property(o => o.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            builder.Property(o => o.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

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
}
