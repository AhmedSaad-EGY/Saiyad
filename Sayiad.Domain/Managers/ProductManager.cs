using Microsoft.Extensions.Logging;
using Sayiad.Data.Common;
using Sayiad.Domain.Dtos.ProductDtos;

namespace Sayiad.Domain.Managers;

public class ProductManager : IProductManager
{
    private readonly IProductRepository _repo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly ILogger<ProductManager> _logger;

    public ProductManager(
        IProductRepository repo,
        ICategoryRepository categoryRepo,
        ILogger<ProductManager> logger)
    {
        _repo = repo;
        _categoryRepo = categoryRepo;
        _logger = logger;
    }

    public async Task<PagedResult<ProductResponse>> GetAllAsync(ProductFilterRequest? filter = null, PaginationRequest? pagination = null)
    {
        var f = filter ?? new ProductFilterRequest(null, null, null, null, null, null);
        var p = pagination ?? new PaginationRequest();
        var result = await _repo.GetAllAsync(f, p);
        return new PagedResult<ProductResponse>
        {
            Items = result.Items.Select(MapToResponse).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }

    public async Task<ProductResponse> GetByIdAsync(int id)
    {
        var product = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Product not found");
        return MapToResponse(product);
    }

    public async Task<ProductResponse> CreateAsync(int sellerId, CreateProductRequest request)
    {
        var category = await _categoryRepo.GetByIdAsync(request.CategoryId)
            ?? throw new KeyNotFoundException("Category not found");

        var product = new Product
        {
            SellerId = sellerId,
            CategoryId = request.CategoryId,
            Title = request.Title,
            Description = request.Description,
            Brand = request.Brand,
            Condition = request.Condition,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            Location = request.Location,
            Status = ProductStatus.Available,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(product);
        _logger.LogInformation("Product created: {ProductId} by seller {SellerId}", product.Id, sellerId);

        return await GetByIdAsync(product.Id);
    }

    public async Task<ProductResponse> UpdateAsync(int id, int sellerId, UpdateProductRequest request)
    {
        var product = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Product not found");

        if (product.SellerId != sellerId)
            throw new UnauthorizedAccessException("You can only edit your own products");

        product.Title = request.Title;
        product.Description = request.Description;
        product.Brand = request.Brand;
        product.Condition = request.Condition;
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;
        product.Location = request.Location;
        product.CategoryId = request.CategoryId;
        product.Status = request.Status;
        product.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(product);
        _logger.LogInformation("Product updated: {ProductId}", id);

        return MapToResponse(product);
    }

    public async Task DeleteAsync(int id, int sellerId)
    {
        var product = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Product not found");

        if (product.SellerId != sellerId)
            throw new UnauthorizedAccessException("You can only delete your own products");

        product.DeletedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(product);
        _logger.LogInformation("Product deleted (soft): {ProductId}", id);
    }

    public async Task<ProductImageResponse> AddImageAsync(
        int productId, int sellerId, AddProductImageRequest request)
    {
        var product = await _repo.GetByIdAsync(productId)
            ?? throw new KeyNotFoundException("Product not found");

        if (product.SellerId != sellerId)
            throw new UnauthorizedAccessException("You can only add images to your own products");

        if (request.IsPrimary)
        {
            foreach (var existing in product.Images.Where(i => i.IsPrimary))
                existing.IsPrimary = false;
        }

        var image = new ProductImage
        {
            ProductId = productId,
            ImageUrl = request.ImageUrl,
            IsPrimary = request.IsPrimary,
            CreatedAt = DateTime.UtcNow
        };

        var saved = await _repo.AddImageAsync(image);
        _logger.LogInformation("Image added to product {ProductId}", productId);

        return new ProductImageResponse(saved.Id, saved.ProductId, saved.ImageUrl, saved.IsPrimary);
    }

    public async Task DeleteImageAsync(int productId, int imageId, int sellerId)
    {
        var image = await _repo.GetImageByIdAsync(imageId)
            ?? throw new KeyNotFoundException("Image not found");

        if (image.ProductId != productId)
            throw new InvalidOperationException("Image does not belong to this product");

        if (image.Product.SellerId != sellerId)
            throw new UnauthorizedAccessException("You can only delete images from your own products");

        await _repo.DeleteImageAsync(image);
        _logger.LogInformation("Image {ImageId} deleted from product {ProductId}", imageId, productId);
    }

    public async Task<IEnumerable<ProductResponse>> GetSellerProductsAsync(int sellerId)
    {
        var products = await _repo.GetSellerProductsAsync(sellerId);
        return products.Select(MapToResponse);
    }

    private static ProductResponse MapToResponse(Product p) => new(
        p.Id, p.Title, p.Description, p.Brand, p.Condition,
        p.Price, p.StockQuantity, p.Location, p.IsAuctioned,
        p.Status, p.SellerId, p.Seller.FullName,
        p.CategoryId, p.Category.Name,
        p.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
        p.CreatedAt, p.UpdatedAt
    );
}
