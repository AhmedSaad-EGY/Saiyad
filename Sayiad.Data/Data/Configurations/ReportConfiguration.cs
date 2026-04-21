namespace Sayiad.Api.Data.Configurations
{
    public class ReportConfiguration : IEntityTypeConfiguration<Report>
    {
        public void Configure(EntityTypeBuilder<Report> builder)
        {
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Reason).IsRequired().HasMaxLength(500);
            builder.Property(r => r.Status).IsRequired().HasMaxLength(20);
            builder.Property(r => r.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
