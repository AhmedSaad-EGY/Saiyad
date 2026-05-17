using Microsoft.EntityFrameworkCore;
using Sayiad.Data.Common;
using Sayiad.Data.Data;
using Sayiad.Data.Repository.WishlistRepo;

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

    public async Task<PagedResult<Wishlist>> GetUserWishlistPagedAsync(int userId, PaginationRequest pagination)
    {
        var query = _db.Wishlists
            .Include(w => w.Product).ThenInclude(p => p.Images)
            .Where(w => w.UserId == userId);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return new PagedResult<Wishlist>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
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
