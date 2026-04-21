namespace Sayiad.Api.Data.Configurations
{
    public class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
    {
        public void Configure(EntityTypeBuilder<Wishlist> builder)
        {
            builder.HasKey(w => w.Id);
            builder.Property(w => w.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
