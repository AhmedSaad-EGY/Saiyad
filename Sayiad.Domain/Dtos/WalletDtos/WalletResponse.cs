namespace Sayiad.Domain.Dtos.WalletDtos;

public record WalletResponse(decimal Balance, decimal HeldBalance, decimal AvailableBalance, DateTime CreatedAt);
public record WalletTransactionResponse(int Id, decimal Amount, string Type, string ReferenceType, int? ReferenceId, string? Description, decimal BalanceSnapshot, DateTime CreatedAt);
public record DepositRequest(decimal Amount);
public class WalletTransactionsResponse
{
    public List<WalletTransactionResponse> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
