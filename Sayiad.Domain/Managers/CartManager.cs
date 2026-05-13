using Microsoft.Extensions.Logging;
using Sayiad.Domain.Dtos.CartDtos;

namespace Sayiad.Domain.Managers;

public class CartManager : ICartManager
{
    private readonly ICartRepository _cartRepo;
    private readonly IProductRepository _productRepo;
    private readonly ILogger<CartManager> _logger;

    public CartManager(ICartRepository cartRepo, IProductRepository productRepo, ILogger<CartManager> logger)
    {
        _cartRepo = cartRepo;
        _productRepo = productRepo;
        _logger = logger;
    }

    public async Task<CartResponse> GetCartAsync(int userId)
    {
        var cart = await GetOrCreateCartAsync(userId);
        return MapToResponse(cart);
    }

    public async Task<CartResponse> AddItemAsync(int userId, AddToCartRequest request)
    {
        var product = await _productRepo.GetByIdAsync(request.ProductId)
            ?? throw new KeyNotFoundException("Product not found");

        if (product.Status != ProductStatus.Available || product.StockQuantity < request.Quantity)
            throw new InvalidOperationException("Product is not available or insufficient stock");

        var cart = await GetOrCreateCartAsync(userId);

        var existingItem = cart.CartItems.FirstOrDefault(i => i.ProductId == request.ProductId);
        if (existingItem != null)
        {
            existingItem.Quantity += request.Quantity;
        }
        else
        {
            cart.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _cartRepo.SaveChangesAsync();
        _logger.LogInformation("Item added to cart: User {UserId}, Product {ProductId}", userId, request.ProductId);

        return await GetCartAsync(userId);
    }

    public async Task<CartResponse> UpdateItemQuantityAsync(int userId, int productId, UpdateCartItemRequest request)
    {
        var cart = await GetOrCreateCartAsync(userId);
        var item = cart.CartItems.FirstOrDefault(i => i.ProductId == productId)
            ?? throw new KeyNotFoundException("Item not found in cart");

        if (request.Quantity <= 0)
        {
            cart.CartItems.Remove(item);
        }
        else
        {
            var product = await _productRepo.GetByIdAsync(productId)
                ?? throw new KeyNotFoundException("Product not found");

            if (product.StockQuantity < request.Quantity)
                throw new InvalidOperationException("Insufficient stock");

            item.Quantity = request.Quantity;
        }

        await _cartRepo.SaveChangesAsync();
        return await GetCartAsync(userId);
    }

    public async Task RemoveItemAsync(int userId, int productId)
    {
        var cart = await _cartRepo.GetCartAsync(userId);
        if (cart != null)
        {
            var item = cart.CartItems.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                cart.CartItems.Remove(item);
                await _cartRepo.SaveChangesAsync();
            }
        }
    }

    public async Task ClearCartAsync(int userId)
    {
        var cart = await _cartRepo.GetCartAsync(userId);
        if (cart != null)
        {
            cart.CartItems.Clear();
            await _cartRepo.SaveChangesAsync();
        }
    }

    private async Task<Cart> GetOrCreateCartAsync(int userId)
    {
        var cart = await _cartRepo.GetCartAsync(userId);
        if (cart == null)
        {
            cart = new Cart
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            await _cartRepo.AddAsync(cart);
        }
        return cart;
    }

    private static CartResponse MapToResponse(Cart cart)
    {
        var items = cart.CartItems.Select(i => new CartItemResponse(
            i.Id, i.ProductId, i.Product.Title, i.Product.Price,
            i.Product.Images.FirstOrDefault(img => img.IsPrimary)?.ImageUrl,
            i.Quantity, i.Product.Price * i.Quantity, i.CreatedAt
        )).ToList();

        return new CartResponse(cart.Id, items, items.Sum(i => i.Subtotal));
    }
}