namespace Sayiad.Data.Models;

public class SystemWalletTransaction
{
    public int Id { get; set; }
    public int SystemWalletId { get; set; }
    public decimal Amount { get; set; }
    public SystemTransactionType Type { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public string? Description { get; set; }
    public decimal BalanceSnapshot { get; set; }
    public bool IsFrozen { get; set; }
    public DateTime CreatedAt { get; set; }

    public SystemWallet SystemWallet { get; set; } = null!;
}
