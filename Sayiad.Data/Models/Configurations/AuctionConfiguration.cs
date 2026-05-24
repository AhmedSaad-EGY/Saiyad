
namespace Sayiad.Data.Models;
    public class AuctionConfiguration : IEntityTypeConfiguration<Auction>
    {
        public void Configure(EntityTypeBuilder<Auction> builder)
        {
            builder.HasKey(a => a.Id);
            builder.Property(a => a.StartingPrice).HasPrecision(18, 2);
            builder.Property(a => a.ReservePrice).HasPrecision(18, 2);
            builder.Property(a => a.MinimumIncrement).HasPrecision(18, 2);
            builder.Property(a => a.CurrentHighestBid).HasPrecision(18, 2);
            builder.Property(a => a.Status).IsRequired().HasMaxLength(20);
            builder.Property(a => a.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            builder.Property(a => a.RowVersion).IsRowVersion();

            builder.HasOne(a => a.CreatedByUser)
                   .WithMany()
                   .HasForeignKey(a => a.CreatedByUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(a => a.Bids)
                   .WithOne(b => b.Auction)
                   .HasForeignKey(b => b.AuctionId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.Winner)
                   .WithMany(u => u.WonAuctions)
                   .HasForeignKey(a => a.WinnerUserId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(a => a.Product)
                   .WithMany(p => p.Auctions)
                   .HasForeignKey(a => a.ProductId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
