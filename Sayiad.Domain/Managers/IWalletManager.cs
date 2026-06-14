using Sayiad.Domain.Dtos.WalletDtos;

namespace Sayiad.Domain.Managers;

public interface IWalletManager
{
    Task<WalletResponse> GetWalletAsync(int userId);
    Task<WalletResponse> DepositAsync(int userId, decimal amount);
    Task<WalletResponse> WithdrawAsync(int userId, decimal amount);
    Task HoldFundsAsync(int userId, decimal amount, string referenceType, int referenceId);
    Task ReleaseHeldFundsAsync(int userId, decimal amount, string referenceType, int referenceId);
    Task DeductForOrderAsync(int userId, decimal amount, int orderId);
    Task CreditSellerAsync(int sellerId, decimal amount, int orderId);
    Task<WalletTransactionsResponse> GetTransactionsAsync(int userId, PaginationRequest pagination);
    Task<bool> HasSufficientBalanceAsync(int userId, decimal amount);
    Task<bool> WalletExistsAsync(int userId);
    Task CreateWalletAsync(int userId);
    Task SettleAuctionPaymentAsync(int winnerId, int sellerId, decimal winningAmount, int auctionId, int auctioneerId);
    Task CreditPlatformFeeAsync(int platformUserId, decimal amount, string referenceType, int referenceId);
    Task DeductForSubscriptionAsync(int userId, decimal amount, int subscriptionId);
    Task ApplyPayoutFreezeAsync(int userId, decimal amount, int freezeDays);
    Task ReverseSellerPayoutAsync(int sellerId, decimal amount, int orderId);
    Task RefundBuyerAsync(int buyerId, decimal amount, int orderId);
    Task ReversePlatformFeeAsync(decimal amount, int orderId);
    Task ReleaseExpiredFreezeAsync(int walletId);
}
