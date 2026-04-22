namespace Sayiad.Data.Data.Configurations
{
    public class SellerProfileConfiguration : IEntityTypeConfiguration<SellerProfile>
    {
        public void Configure(EntityTypeBuilder<SellerProfile> builder)
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.StoreName).IsRequired().HasMaxLength(150);
            builder.Property(s => s.StoreDescription).HasMaxLength(500);
            builder.Property(s => s.AverageRating).HasPrecision(3, 2).HasDefaultValue(0);
            builder.Property(s => s.TotalSales).HasDefaultValue(0);
            builder.Property(s => s.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
