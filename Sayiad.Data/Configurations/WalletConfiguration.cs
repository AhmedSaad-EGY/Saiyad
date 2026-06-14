namespace Sayiad.Data.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("Wallets");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Balance).HasColumnType("decimal(18,2)");
        builder.Property(w => w.HeldBalance).HasColumnType("decimal(18,2)");
        builder.Property(w => w.RowVersion).IsRowVersion();
        builder.Property(w => w.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(w => w.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(w => w.User)
            .WithOne(u => u.Wallet)
            .HasForeignKey<Wallet>(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(w => w.UserId).IsUnique();
        builder.HasIndex(w => w.FreezeUntil).HasFilter("[FreezeUntil] IS NOT NULL");
    }
}

public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.ToTable("WalletTransactions");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Amount).HasColumnType("decimal(18,2)");
        builder.Property(t => t.Type)
            .HasMaxLength(25)
            .IsRequired()
            .HasConversion<string>();
        builder.Property(t => t.ReferenceType).HasMaxLength(50).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.BalanceSnapshot).HasColumnType("decimal(18,2)");
        builder.Property(t => t.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(t => t.Wallet)
            .WithMany(w => w.Transactions)
            .HasForeignKey(t => t.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.WalletId);
        builder.HasIndex(t => t.CreatedAt);
    }
}
