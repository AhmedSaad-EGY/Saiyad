using Sayiad.Data.Data;
using Sayiad.Data.Repository.CartRepo;

namespace Sayiad.Data.Repository.CartRepo;

public class CartRepository : ICartRepository
{
    private readonly ApplicationDbContext _db;

    public CartRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Cart?> GetCartAsync(int userId)
    {
        return await _db.Carts
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                    .ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task AddAsync(Cart cart)
    {
        _db.Carts.Add(cart);
        await _db.SaveChangesAsync();
    }

    public async Task ClearCartAsync(int userId)
    {
        var cart = await _db.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart != null)
        {
            _db.CartItems.RemoveRange(cart.CartItems);
            await _db.SaveChangesAsync();
        }
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}
