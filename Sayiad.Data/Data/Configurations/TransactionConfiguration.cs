namespace Sayiad.Api.Data.Configurations
{
    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.HasKey(t => t.Id);
            builder.Property(t => t.TransactionReference).IsRequired().HasMaxLength(100);
            builder.Property(t => t.Amount).HasPrecision(18, 2);
            builder.Property(t => t.Status).IsRequired().HasMaxLength(20);
            builder.Property(t => t.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
