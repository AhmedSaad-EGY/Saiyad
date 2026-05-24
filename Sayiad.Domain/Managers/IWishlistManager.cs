using Sayiad.Data.Common;
using Sayiad.Domain.Dtos.WishlistDtos;

namespace Sayiad.Domain.Managers;

public interface IWishlistManager
{
    Task<IEnumerable<WishlistItemResponse>> GetWishlistAsync(int userId);
    Task<PagedResult<WishlistItemResponse>> GetWishlistPagedAsync(int userId, PaginationRequest pagination);
    Task<(WishlistItemResponse? Item, bool Added)> ToggleAsync(int userId, ToggleWishlistRequest request);
    Task RemoveAsync(int userId, int productId);
}
