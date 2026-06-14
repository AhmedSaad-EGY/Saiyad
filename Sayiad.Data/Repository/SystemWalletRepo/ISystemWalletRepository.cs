namespace Sayiad.Data.Repository.SystemWalletRepo;

public interface ISystemWalletRepository
{
    Task<SystemWallet?> GetWithLockAsync();
    Task<SystemWallet> GetOrThrowAsync();
    Task AddTransactionAsync(SystemWalletTransaction txn);
    Task<IEnumerable<SystemWalletTransaction>> GetTransactionsAsync(int page, int pageSize);
    Task<IEnumerable<SystemWalletTransaction>> GetExpiredFrozenTransactionsAsync();
}
