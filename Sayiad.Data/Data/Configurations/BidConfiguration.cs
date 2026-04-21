namespace Sayiad.Api.Data.Configurations
{
    public class BidConfiguration : IEntityTypeConfiguration<Bid>
    {
        public void Configure(EntityTypeBuilder<Bid> builder)
        {
            builder.HasKey(b => b.Id);
            builder.Property(b => b.Amount).HasPrecision(18, 2);
            builder.Property(b => b.BidStatus).IsRequired().HasMaxLength(20);
            builder.Property(b => b.IsAutoBid).HasDefaultValue(false);
            builder.Property(b => b.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
