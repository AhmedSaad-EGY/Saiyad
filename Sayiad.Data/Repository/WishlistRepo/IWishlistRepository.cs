using Sayiad.Data.Common;

namespace Sayiad.Data.Repository.WishlistRepo;

public interface IWishlistRepository
{
    Task<IEnumerable<Wishlist>> GetUserWishlistAsync(int userId);
    Task<PagedResult<Wishlist>> GetUserWishlistPagedAsync(int userId, PaginationRequest pagination);
    Task<Wishlist?> GetByUserAndProductAsync(int userId, int productId);
    Task AddAsync(Wishlist wishlist);
    Task RemoveAsync(Wishlist wishlist);
}
