namespace Sayiad.Data.Models;

public class WalletTransaction
{
    public int Id { get; set; }
    public int WalletId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string ReferenceType { get; set; } = null!;
    public int? ReferenceId { get; set; }
    public string? Description { get; set; }
    public decimal BalanceSnapshot { get; set; }
    public DateTime CreatedAt { get; set; }

    public Wallet Wallet { get; set; } = null!;
}
