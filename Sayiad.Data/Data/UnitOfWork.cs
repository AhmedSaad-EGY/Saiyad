using Microsoft.EntityFrameworkCore.Storage;

namespace Sayiad.Data.Data;

public class UnitOfWork(ApplicationDbContext db) : IUnitOfWork
{
    private readonly ApplicationDbContext _db = db;

    public IDbContextTransaction? CurrentTransaction => _db.Database.CurrentTransaction;

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
        => await _db.Database.BeginTransactionAsync(ct);
}
