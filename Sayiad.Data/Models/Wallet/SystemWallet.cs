namespace Sayiad.Data.Models;

public class SystemWallet
{
    public int Id { get; set; }
    public decimal Balance { get; set; }
    public decimal HeldBalance { get; set; }
    public decimal AvailableBalance => Balance - HeldBalance;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = null!;

    public ICollection<SystemWalletTransaction> Transactions { get; set; } = new List<SystemWalletTransaction>();
}
