namespace Sayiad.Data.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);
            builder.HasIndex(u => u.Email).IsUnique();
            builder.Property(u => u.FullName).IsRequired().HasMaxLength(150);
            builder.Property(u => u.Email).IsRequired().HasMaxLength(100);
            builder.Property(u => u.PasswordHash).IsRequired();
            builder.Property(u => u.Role).IsRequired();
            builder.Property(u => u.IsActive).HasDefaultValue(true);
            builder.Property(u => u.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            builder.Property(u => u.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

            builder.HasMany(u => u.Products)
                   .WithOne(p => p.Seller)
                   .HasForeignKey(p => p.SellerId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.CustomerOrders)
                   .WithOne(o => o.Buyer)
                   .HasForeignKey(o => o.BuyerId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.Bids)
                   .WithOne(b => b.User)
                   .HasForeignKey(b => b.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.Reviews)
                   .WithOne(r => r.User)
                   .HasForeignKey(r => r.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.ShippingAddresses)
                   .WithOne(a => a.User)
                   .HasForeignKey(a => a.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(u => u.Cart)
                   .WithOne(c => c.User)
                   .HasForeignKey<Cart>(c => c.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.Wishlists)
                   .WithOne(w => w.User)
                   .HasForeignKey(w => w.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.Notifications)
                   .WithOne(n => n.User)
                   .HasForeignKey(n => n.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.Reports)
                   .WithOne(r => r.Reporter)
                   .HasForeignKey(r => r.ReporterId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(u => u.SellerProfile)
                   .WithOne(s => s.User)
                   .HasForeignKey<SellerProfile>(s => s.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.WonAuctions)
                   .WithOne(a => a.Winner)
                   .HasForeignKey(a => a.WinnerUserId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(u => u.SoldOrderItems)
                   .WithOne(oi => oi.Seller)
                   .HasForeignKey(oi => oi.SellerId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

}
