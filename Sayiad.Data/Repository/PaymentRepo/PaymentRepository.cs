using Sayiad.Data.Repository.PaymentRepo;
using Sayiad.Data.Data;

namespace Sayiad.Data.Repository.PaymentRepo;

public class PaymentRepository : IPaymentRepository
{
    private readonly ApplicationDbContext _db;

    public PaymentRepository(ApplicationDbContext db) => _db = db;

    public async Task<Payment?> GetByIdAsync(int id)
    {
        return await _db.Payments
            .Include(p => p.Transactions)
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<Payment>> GetOrderPaymentsAsync(int orderId)
    {
        return await _db.Payments
            .Include(p => p.Transactions)
            .Include(p => p.Order)
            .Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Payment payment)
    {
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Payment payment)
    {
        _db.Payments.Update(payment);
        await _db.SaveChangesAsync();
    }
}
