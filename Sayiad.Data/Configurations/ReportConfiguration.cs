namespace Sayiad.Data.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("Reports");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Type)
            .HasMaxLength(30)
            .HasConversion<string>();
        builder.Property(r => r.TargetType)
            .HasMaxLength(30)
            .HasConversion<string>();
        builder.Property(r => r.Status)
            .HasMaxLength(30)
            .HasConversion<string>();
        builder.Property(r => r.Message).HasMaxLength(2000).IsRequired();
        builder.Property(r => r.AdminNote).HasMaxLength(1000);
        builder.Property(r => r.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(r => r.Reporter)
            .WithMany()
            .HasForeignKey(r => r.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.ReporterId);
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.CreatedAt);
        builder.HasIndex(r => new { r.TargetType, r.TargetId });
    }
}
