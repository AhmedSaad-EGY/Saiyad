namespace Sayiad.Data.Configurations;

public class AuctionRequestConfiguration : IEntityTypeConfiguration<AuctionRequest>
{
    public void Configure(EntityTypeBuilder<AuctionRequest> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProductTitle).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ProductDescription).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.FishType).IsRequired().HasMaxLength(100);
        builder.Property(x => x.CatchLocation).IsRequired().HasMaxLength(200);
        builder.Property(x => x.EstimatedValue).HasPrecision(18, 2);
        builder.Property(x => x.QuantityKg).HasPrecision(10, 2);
        builder.Property(x => x.Status).HasConversion<int>();

        builder.HasOne(x => x.Fisherman)
            .WithMany()
            .HasForeignKey(x => x.FishermanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReviewedByAuctioneer)
            .WithMany()
            .HasForeignKey(x => x.ReviewedByAuctioneerId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(x => x.ResultingAuction)
            .WithMany()
            .HasForeignKey(x => x.ResultingAuctionId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
