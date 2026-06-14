using Sayiad.Data.Data;

namespace Sayiad.Data.Repository.WalletRepo;

public class WalletRepository : IWalletRepository
{
    private readonly ApplicationDbContext _db;

    public WalletRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Wallet?> GetByUserIdAsync(int userId)
    {
        return await _db.Set<Wallet>()
            .Include(w => w.Transactions)
            .FirstOrDefaultAsync(w => w.UserId == userId);
    }

    public async Task<Wallet> CreateAsync(Wallet wallet)
    {
        _db.Set<Wallet>().Add(wallet);
        await _db.SaveChangesAsync();
        return wallet;
    }

    public async Task<Wallet> UpdateAsync(Wallet wallet)
    {
        _db.Set<Wallet>().Update(wallet);
        await _db.SaveChangesAsync();
        return wallet;
    }

    public Task AddTransactionAsync(WalletTransaction transaction)
    {
        _db.Set<WalletTransaction>().Add(transaction);
        return Task.CompletedTask;
    }

    public async Task<List<WalletTransaction>> GetTransactionsAsync(int walletId, PaginationRequest pagination)
    {
        return await _db.Set<WalletTransaction>()
            .Where(t => t.WalletId == walletId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();
    }

    public async Task<int> GetTransactionCountAsync(int walletId)
    {
        return await _db.Set<WalletTransaction>()
            .CountAsync(t => t.WalletId == walletId);
    }

    public async Task<Wallet?> GetByUserIdWithLockAsync(int userId)
    {
        return await _db.Set<Wallet>()
            .FromSqlInterpolated($"SELECT * FROM Wallets WITH (UPDLOCK, ROWLOCK) WHERE UserId = {userId}")
            .FirstOrDefaultAsync();
    }

    public async Task<List<Wallet>> GetExpiredFrozenWalletsAsync()
    {
        return await _db.Wallets
            .Where(w => w.FreezeUntil != null
                     && w.FreezeUntil <= DateTime.UtcNow
                     && w.HeldBalance > 0)
            .ToListAsync();
    }

    public async Task<int> CountExpiredFrozenWalletsAsync()
    {
        return await _db.Wallets
            .CountAsync(w => w.FreezeUntil != null
                          && w.FreezeUntil <= DateTime.UtcNow
                          && w.HeldBalance > 0);
    }
}
