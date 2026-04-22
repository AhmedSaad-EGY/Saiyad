namespace Sayiad.Data.Data.Configurations
{
    public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
    {
        public void Configure(EntityTypeBuilder<ProductImage> builder)
        {
            builder.HasKey(pi => pi.Id);
            builder.Property(pi => pi.ImageUrl).IsRequired();
            builder.Property(pi => pi.IsPrimary).HasDefaultValue(false);
            builder.Property(pi => pi.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
