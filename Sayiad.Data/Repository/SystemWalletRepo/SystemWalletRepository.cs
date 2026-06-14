using Sayiad.Data.Data;

namespace Sayiad.Data.Repository.SystemWalletRepo;

public class SystemWalletRepository : ISystemWalletRepository
{
    private readonly ApplicationDbContext _db;

    public SystemWalletRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<SystemWallet?> GetWithLockAsync()
    {
        return await _db.SystemWallets
            .FromSqlRaw("SELECT * FROM SystemWallets WITH (UPDLOCK, ROWLOCK)")
            .FirstOrDefaultAsync();
    }

    public async Task<SystemWallet> GetOrThrowAsync()
    {
        return await _db.SystemWallets
            .Include(w => w.Transactions)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("System wallet not initialized");
    }

    public Task AddTransactionAsync(SystemWalletTransaction txn)
    {
        _db.SystemWalletTransactions.Add(txn);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<SystemWalletTransaction>> GetTransactionsAsync(int page, int pageSize)
    {
        return await _db.SystemWalletTransactions
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<SystemWalletTransaction>> GetExpiredFrozenTransactionsAsync()
    {
        return await _db.SystemWalletTransactions
            .Where(t => t.IsFrozen)
            .ToListAsync();
    }
}
