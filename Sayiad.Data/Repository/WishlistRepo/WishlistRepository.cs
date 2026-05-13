using Microsoft.EntityFrameworkCore;
using Sayiad.Data.Repository.WishlistRepo;
using Sayiad.Data.Data;

namespace Sayiad.Data.Repository.WishlistRepo;

public class WishlistRepository : IWishlistRepository
{
    private readonly ApplicationDbContext _db;

    public WishlistRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Wishlist>> GetUserWishlistAsync(int userId)
    {
        return await _db.Wishlists
            .Include(w => w.Product).ThenInclude(p => p.Images)
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();
    }

    public async Task<Wishlist?> GetByUserAndProductAsync(int userId, int productId)
    {
        return await _db.Wishlists
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);
    }

    public async Task AddAsync(Wishlist wishlist)
    {
        _db.Wishlists.Add(wishlist);
        await _db.SaveChangesAsync();
    }

    public async Task RemoveAsync(Wishlist wishlist)
    {
        _db.Wishlists.Remove(wishlist);
        await _db.SaveChangesAsync();
    }
}
