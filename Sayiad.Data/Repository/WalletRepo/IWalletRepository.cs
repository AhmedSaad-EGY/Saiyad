namespace Sayiad.Data.Repository.WalletRepo;

public interface IWalletRepository
{
    Task<Wallet?> GetByUserIdAsync(int userId);
    Task<Wallet> CreateAsync(Wallet wallet);
    Task<Wallet> UpdateAsync(Wallet wallet);
    Task AddTransactionAsync(WalletTransaction transaction);
    Task<List<WalletTransaction>> GetTransactionsAsync(int walletId, PaginationRequest pagination);
    Task<int> GetTransactionCountAsync(int walletId);
    Task<Wallet?> GetByUserIdWithLockAsync(int userId);
    Task<List<Wallet>> GetExpiredFrozenWalletsAsync();
    Task<int> CountExpiredFrozenWalletsAsync();
}
