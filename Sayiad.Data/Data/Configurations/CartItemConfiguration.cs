namespace Sayiad.Api.Data.Configurations
{
    public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
    {
        public void Configure(EntityTypeBuilder<CartItem> builder)
        {
            builder.HasKey(ci => ci.Id);
            builder.Property(ci => ci.Quantity).IsRequired();
            builder.Property(ci => ci.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
