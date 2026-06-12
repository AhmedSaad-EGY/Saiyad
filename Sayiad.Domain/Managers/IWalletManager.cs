using Sayiad.Domain.Dtos.WalletDtos;

namespace Sayiad.Domain.Managers;

public interface IWalletManager
{
    Task<WalletResponse> GetWalletAsync(int userId);
    Task<WalletResponse> DepositAsync(int userId, decimal amount);
    Task<WalletResponse> WithdrawAsync(int userId, decimal amount);
    Task HoldFundsAsync(int userId, decimal amount, string referenceType, int referenceId);
    Task ReleaseHeldFundsAsync(int userId, decimal amount, string referenceType, int referenceId);
    Task TransferFundsAsync(int fromUserId, int toUserId, decimal amount, string description);
    Task DeductForOrderAsync(int userId, decimal amount, int orderId);
    Task CreditSellerAsync(int sellerId, decimal amount, int orderId);
    Task<WalletTransactionsResponse> GetTransactionsAsync(int userId, PaginationRequest pagination);
    Task<bool> HasSufficientBalanceAsync(int userId, decimal amount);
    Task<bool> WalletExistsAsync(int userId);
    Task CreateWalletAsync(int userId);
    Task SettleAuctionPaymentAsync(int winnerId, int sellerId, decimal winningAmount, int auctionId);
    Task CreditPlatformFeeAsync(int platformUserId, decimal amount, string referenceType, int referenceId);
    Task DeductForSubscriptionAsync(int userId, decimal amount, int subscriptionId);
}
