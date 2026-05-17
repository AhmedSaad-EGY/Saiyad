using Microsoft.Extensions.Logging;
using Sayiad.Data.Common;
using Sayiad.Domain.Dtos.WishlistDtos;

namespace Sayiad.Domain.Managers;

public class WishlistManager : IWishlistManager
{
    private readonly IWishlistRepository _wishlistRepo;
    private readonly IProductRepository _productRepo;
    private readonly ILogger<WishlistManager> _logger;

    public WishlistManager(IWishlistRepository wishlistRepo, IProductRepository productRepo, ILogger<WishlistManager> logger)
    {
        _wishlistRepo = wishlistRepo;
        _productRepo = productRepo;
        _logger = logger;
    }

    public async Task<IEnumerable<WishlistItemResponse>> GetWishlistAsync(int userId)
    {
        var items = await _wishlistRepo.GetUserWishlistAsync(userId);
        return items.Select(MapItem);
    }

    public async Task<PagedResult<WishlistItemResponse>> GetWishlistPagedAsync(int userId, PaginationRequest pagination)
    {
        var result = await _wishlistRepo.GetUserWishlistPagedAsync(userId, pagination);
        return new PagedResult<WishlistItemResponse>
        {
            Items = result.Items.Select(MapItem).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }

    public async Task<(WishlistItemResponse? Item, bool Added)> ToggleAsync(int userId, ToggleWishlistRequest request)
    {
        var existing = await _wishlistRepo.GetByUserAndProductAsync(userId, request.ProductId);

        if (existing != null)
        {
            await _wishlistRepo.RemoveAsync(existing);
            _logger.LogInformation("Removed from wishlist: User {UserId}, Product {ProductId}", userId, request.ProductId);
            return (null, false);
        }

        var product = await _productRepo.GetByIdAsync(request.ProductId)
            ?? throw new KeyNotFoundException("Product not found");

        var wishlistItem = new Wishlist
        {
            UserId = userId,
            ProductId = request.ProductId,
            CreatedAt = DateTime.UtcNow
        };

        await _wishlistRepo.AddAsync(wishlistItem);

        _logger.LogInformation("Added to wishlist: User {UserId}, Product {ProductId}", userId, request.ProductId);

        var response = new WishlistItemResponse(
            wishlistItem.Id, product.Id, product.Title, product.Price,
            product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
            wishlistItem.CreatedAt);

        return (response, true);
    }

    public async Task RemoveAsync(int userId, int productId)
    {
        var item = await _wishlistRepo.GetByUserAndProductAsync(userId, productId);
        if (item != null)
        {
            await _wishlistRepo.RemoveAsync(item);
        }
    }

    private static WishlistItemResponse MapItem(Wishlist w) => new(
        w.Id, w.ProductId, w.Product.Title, w.Product.Price,
        w.Product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
        w.CreatedAt);
}