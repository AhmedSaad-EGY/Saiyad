using Microsoft.EntityFrameworkCore;
using Sayiad.Data.Common;
using Sayiad.Data.Repository.ProductRepo;
using Sayiad.Data.Data;

namespace Sayiad.Data.Repository.ProductRepo;

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _db;

    public ProductRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<Product>> GetAllAsync(ProductFilterRequest filter, PaginationRequest pagination)
    {
        var query = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Where(p => p.DeletedAt == null && p.Status == ProductStatus.Available)
            .AsQueryable();

        if (filter.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == filter.CategoryId);
        if (filter.MinPrice.HasValue)
            query = query.Where(p => p.Price >= filter.MinPrice);
        if (filter.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= filter.MaxPrice);
        if (filter.Condition.HasValue)
            query = query.Where(p => p.Condition == filter.Condition);
        if (!string.IsNullOrWhiteSpace(filter.Location))
            query = query.Where(p => p.Location.Contains(filter.Location));
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            query = query.Where(p =>
                p.Title.Contains(filter.SearchTerm) ||
                p.Description.Contains(filter.SearchTerm));

        if (filter.InStock == true)
            query = query.Where(p => p.StockQuantity > 0);

        if (filter.SellerId.HasValue)
            query = query.Where(p => p.SellerId == filter.SellerId);

            if (filter.IsAuctioned.HasValue)
            query = query.Where(p => p.IsAuctioned == filter.IsAuctioned.Value);

        var totalCount = await query.CountAsync();

        IOrderedQueryable<Product> ordered = (filter.SortBy?.ToLower()) switch
        {
            "price" => filter.SortDirection?.ToLower() == "asc"
                ? query.OrderBy(p => p.Price)
                : query.OrderByDescending(p => p.Price),
            "title" => filter.SortDirection?.ToLower() == "asc"
                ? query.OrderBy(p => p.Title)
                : query.OrderByDescending(p => p.Title),
            _ => filter.SortDirection?.ToLower() == "asc"
                ? query.OrderBy(p => p.CreatedAt)
                : query.OrderByDescending(p => p.CreatedAt),
        };

        var items = await ordered
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return new PagedResult<Product>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);
    }

    public async Task<IEnumerable<Product>> GetSellerProductsAsync(int sellerId)
    {
        return await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Where(p => p.SellerId == sellerId && p.DeletedAt == null)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Product product)
    {
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Product product)
    {
        _db.Products.Update(product);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _db.Products.AnyAsync(p => p.Id == id && p.DeletedAt == null);
    }

    public async Task<ProductImage> AddImageAsync(ProductImage image)
    {
        if (image.IsPrimary)
        {
            await _db.ProductImages
                .Where(i => i.ProductId == image.ProductId && i.IsPrimary)
                .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.IsPrimary, false));
        }

        await _db.ProductImages.AddAsync(image);
        await _db.SaveChangesAsync();
        return image;
    }

    public async Task<ProductImage?> GetImageByIdAsync(int imageId)
    {
        return await _db.ProductImages
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.Id == imageId);
    }

    public async Task DeleteImageAsync(ProductImage image)
    {
        _db.ProductImages.Remove(image);
        await _db.SaveChangesAsync();
    }

    public async Task<PagedResult<Product>> GetPendingReviewAsync(PaginationRequest pagination)
    {
        var query = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Seller)
            .Where(p => p.DeletedAt == null && p.Status == ProductStatus.PendingReview)
            .OrderBy(p => p.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return new PagedResult<Product>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }
}
