namespace Sayiad.Data.Configurations;

public class SystemWalletConfiguration : IEntityTypeConfiguration<SystemWallet>
{
    public void Configure(EntityTypeBuilder<SystemWallet> builder)
    {
        builder.ToTable("SystemWallets");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Balance).HasColumnType("decimal(18,2)");
        builder.Property(w => w.HeldBalance).HasColumnType("decimal(18,2)");
        builder.Property(w => w.RowVersion).IsRowVersion();
        builder.Property(w => w.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(w => w.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
    }
}

public class SystemWalletTransactionConfiguration : IEntityTypeConfiguration<SystemWalletTransaction>
{
    public void Configure(EntityTypeBuilder<SystemWalletTransaction> builder)
    {
        builder.ToTable("SystemWalletTransactions");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Amount).HasColumnType("decimal(18,2)");
        builder.Property(t => t.Type)
            .HasMaxLength(50)
            .HasConversion<string>();
        builder.Property(t => t.ReferenceType).HasMaxLength(50);
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.BalanceSnapshot).HasColumnType("decimal(18,2)");
        builder.Property(t => t.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(t => t.SystemWallet)
            .WithMany(w => w.Transactions)
            .HasForeignKey(t => t.SystemWalletId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.SystemWalletId);
        builder.HasIndex(t => t.CreatedAt);
    }
}
