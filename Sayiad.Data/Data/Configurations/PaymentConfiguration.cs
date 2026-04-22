namespace Sayiad.Data.Data.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Amount).HasPrecision(18, 2);
            builder.Property(p => p.PaymentMethod).IsRequired().HasMaxLength(50);
            builder.Property(p => p.PaymentStatus).IsRequired().HasMaxLength(20);
            builder.Property(p => p.PaidAt).IsRequired(false);
            builder.Property(p => p.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            builder.HasMany(p => p.Transactions)
                   .WithOne(t => t.Payment)
                   .HasForeignKey(t => t.PaymentId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
