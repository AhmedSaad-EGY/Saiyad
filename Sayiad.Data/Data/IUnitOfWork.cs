using Microsoft.EntityFrameworkCore.Storage;

namespace Sayiad.Data.Data;

public interface IUnitOfWork
{
    IDbContextTransaction? CurrentTransaction { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
}
