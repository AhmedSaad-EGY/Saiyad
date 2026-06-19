using Microsoft.Extensions.Logging;
using Sayiad.Data.Common;
using Sayiad.Domain.Common;
using Sayiad.Domain.Dtos.ProductDtos;

namespace Sayiad.Domain.Managers;

public class ProductManager : IProductManager
{
    private readonly IProductRepository _repo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly ILogger<ProductManager> _logger;
    private readonly INotificationManager _notificationManager;
    private readonly IUserRepository _userRepo;

    public ProductManager(
        IProductRepository repo,
        ICategoryRepository categoryRepo,
        ILogger<ProductManager> logger,
        INotificationManager notificationManager,
        IUserRepository userRepo)
    {
        _repo = repo;
        _categoryRepo = categoryRepo;
        _logger = logger;
        _notificationManager = notificationManager;
        _userRepo = userRepo;
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

    public async Task<PagedResult<ProductResponse>> GetAllForAdminAsync(PaginationRequest? pagination = null)
    {
        var p = pagination ?? new PaginationRequest();
        var result = await _repo.GetAllForAdminAsync(p);
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
            Title = InputSanitizer.Sanitize(request.Title),
            Description = InputSanitizer.Sanitize(request.Description),
            Brand = InputSanitizer.SanitizeNullable(request.Brand) ?? string.Empty,
            Condition = request.Condition,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            Location = InputSanitizer.SanitizeNullable(request.Location) ?? string.Empty,
            Status = ProductStatus.PendingReview,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(product);

        _logger.LogInformation(
            "Product created: {ProductId} by seller {SellerId}",
            product.Id, sellerId);

        // Notify all admins about new product pending review
        var admins = await _userRepo.GetUsersByRoleAsync(UserRole.Admin);
        foreach (var admin in admins)
        {
            await _notificationManager.CreateAsync(admin.Id, "New Product Review",
                $"Product '{product.Title}' needs your review.");
        }

        return await GetByIdAsync(product.Id);
    }

    public async Task<ProductResponse> UpdateAsync(int id, int sellerId, UpdateProductRequest request)
    {
        var product = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Product not found");

        if (product.SellerId != sellerId)
            throw new UnauthorizedAccessException("You can only edit your own products");

        product.Title = InputSanitizer.Sanitize(request.Title);
        product.Description = InputSanitizer.Sanitize(request.Description);
        product.Brand = InputSanitizer.SanitizeNullable(request.Brand) ?? string.Empty;
        product.Condition = request.Condition;
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;
        product.Location = InputSanitizer.SanitizeNullable(request.Location) ?? string.Empty;
        product.CategoryId = request.CategoryId;

        // Seller edits to a live product must be reviewed again before staying public.
        if (product.Status == ProductStatus.Available)
        {
            product.Status = ProductStatus.PendingReview;
            product.ReviewedByUserId = null;
            product.ReviewedAt = null;
            product.RejectionReason = null;
        }

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

    public async Task<ProductResponse> UpdateStatusAsync(int id, ProductStatus status)
    {
        var product = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Product not found");

        product.Status = status;
        product.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(product);
        _logger.LogInformation("Product {ProductId} status updated to {Status} by admin", id, status);

        return MapToResponse(product);
    }

    public async Task<ProductResponse> UpdateSellerStatusAsync(int productId, int sellerId, ProductStatus newStatus)
    {
        var product = await _repo.GetByIdAsync(productId)
            ?? throw new KeyNotFoundException("Product not found");

        if (product.SellerId != sellerId)
            throw new UnauthorizedAccessException("You can only update your own products");

        var allowed = (product.Status, newStatus) switch
        {
            (ProductStatus.Available, ProductStatus.Draft) => true,
            (ProductStatus.Available, ProductStatus.Sold) => true,
            (ProductStatus.Draft, ProductStatus.Available) => true,
            (ProductStatus.Draft, ProductStatus.Sold) => true,
            _ => false
        };

        if (!allowed)
            throw new InvalidOperationException(
                $"Cannot change status from '{product.Status}' to '{newStatus}'. " +
                $"Allowed transitions: Available ↔ Draft, Available → Sold, Draft → Sold.");

        product.Status = newStatus;
        product.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(product);
        _logger.LogInformation("Product {ProductId} status changed to {Status} by seller {SellerId}", productId, newStatus, sellerId);

        return MapToResponse(product);
    }

    public async Task<IEnumerable<ProductResponse>> GetSellerProductsAsync(int sellerId)
    {
        var products = await _repo.GetSellerProductsAsync(sellerId);
        return products.Select(MapToResponse);
    }

    public async Task<PagedResult<ProductResponse>> GetMyProductsAsync(int sellerId, PaginationRequest pagination)
    {
        var p = pagination ?? new PaginationRequest();
        var result = await _repo.GetSellerProductsPagedAsync(sellerId, p);
        return new PagedResult<ProductResponse>
        {
            Items = result.Items.Select(MapToResponse).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }

    public async Task<PagedResult<ProductResponse>> GetPendingReviewAsync(PaginationRequest pagination)
    {
        var p = pagination ?? new PaginationRequest();
        var result = await _repo.GetPendingReviewAsync(p);
        return new PagedResult<ProductResponse>
        {
            Items = result.Items.Select(MapToResponse).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }

    public async Task<ProductResponse> ApproveProductAsync(int id, int adminId)
    {
        var product = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Product not found");

        if (product.Status != ProductStatus.PendingReview)
            throw new InvalidOperationException("Only pending-review products can be approved");

        product.Status = ProductStatus.Available;
        product.ReviewedByUserId = adminId;
        product.ReviewedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(product);
        _logger.LogInformation("Product {ProductId} approved by admin {AdminId}", id, adminId);

        // Notify seller their product was approved
        await _notificationManager.CreateAsync(product.SellerId, "Product Approved",
            $"Your product '{product.Title}' has been approved and is now live.");

        return MapToResponse(product);
    }

    public async Task<ProductResponse> RejectProductAsync(int id, int adminId, string reason)
    {
        var product = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Product not found");

        if (product.Status != ProductStatus.PendingReview)
            throw new InvalidOperationException("Only pending-review products can be rejected");

        product.Status = ProductStatus.Rejected;
        product.ReviewedByUserId = adminId;
        product.ReviewedAt = DateTime.UtcNow;
        product.RejectionReason = reason;
        product.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(product);
        _logger.LogInformation(
            "Product {ProductId} rejected by admin {AdminId}: {Reason}",
            id, adminId, reason);

        // Notify seller their product was rejected
        await _notificationManager.CreateAsync(product.SellerId, "Product Rejected",
            $"Your product '{product.Title}' has been rejected. Reason: {reason}");

        return MapToResponse(product);
    }

    private static ProductResponse MapToResponse(Product p) => new(
        p.Id, p.Title, p.Description, p.Brand, p.Condition,
        p.Price, p.StockQuantity, p.Location, p.IsAuctioned,
        p.Auctions?.FirstOrDefault(a => a.Status == AuctionStatus.Active)?.Id,
        p.Status, p.SellerId, p.Seller?.FullName ?? string.Empty,
        p.CategoryId, p.Category.Name,
        p.Images?.FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
        p.CreatedAt, p.UpdatedAt,
        p.ReviewedByUserId, p.ReviewedAt, p.RejectionReason,
        p.Images?.Select(i => new ProductImageResponse(i.Id, i.ProductId, i.ImageUrl, i.IsPrimary)).ToList()
    );
}
