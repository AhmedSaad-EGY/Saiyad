using Sayiad.Data.Data;
using Sayiad.Data.Repository.ShippingAddressRepo;

namespace Sayiad.Data.Repository.ShippingAddressRepo;

public class ShippingAddressRepository : IShippingAddressRepository
{
    private readonly ApplicationDbContext _db;

    public ShippingAddressRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ShippingAddress> CreateAsync(ShippingAddress address)
    {
        _db.ShippingAddresses.Add(address);
        await _db.SaveChangesAsync();
        return address;
    }

    public async Task<ShippingAddress?> GetByIdAsync(int id)
    {
        return await _db.ShippingAddresses.FindAsync(id);
    }

    public async Task<List<ShippingAddress>> GetByUserIdAsync(int userId)
    {
        return await _db.ShippingAddresses
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var address = await _db.ShippingAddresses.FindAsync(id);
        if (address == null) return false;

        _db.ShippingAddresses.Remove(address);
        await _db.SaveChangesAsync();
        return true;
    }
}
