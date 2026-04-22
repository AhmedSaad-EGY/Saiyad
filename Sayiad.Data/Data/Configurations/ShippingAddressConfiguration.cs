namespace Sayiad.Data.Data.Configurations
{
    public class ShippingAddressConfiguration : IEntityTypeConfiguration<ShippingAddress>
    {
        public void Configure(EntityTypeBuilder<ShippingAddress> builder)
        {
            builder.HasKey(sa => sa.Id);
            builder.Property(sa => sa.FullName).IsRequired().HasMaxLength(150);
            builder.Property(sa => sa.Phone).IsRequired().HasMaxLength(20);
            builder.Property(sa => sa.City).IsRequired().HasMaxLength(50);
            builder.Property(sa => sa.AddressLine).IsRequired().HasMaxLength(200);
            builder.Property(sa => sa.PostalCode).IsRequired().HasMaxLength(20);
            builder.Property(sa => sa.IsDefault).HasDefaultValue(false);
            builder.Property(sa => sa.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
