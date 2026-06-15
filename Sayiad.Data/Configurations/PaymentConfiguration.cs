namespace Sayiad.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Amount).HasColumnType("decimal(18,2)");
        builder.Property(p => p.PaymentMethod).HasMaxLength(50).IsRequired();
        builder.Property(p => p.PaymentStatus)
            .HasMaxLength(20)
            .HasConversion(
                v => v == Models.PaymentStatus.Confirmed ? "Paid" : v.ToString(),
                v => v == "Paid" ? Models.PaymentStatus.Confirmed : Enum.Parse<Models.PaymentStatus>(v));
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(p => p.RowVersion).IsRowVersion();

        builder.HasOne(p => p.Order)
            .WithMany(o => o.Payments)
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.FreezeUntil).HasFilter("[FreezeUntil] IS NOT NULL");
        builder.HasIndex(p => p.OrderId);
    }
}
