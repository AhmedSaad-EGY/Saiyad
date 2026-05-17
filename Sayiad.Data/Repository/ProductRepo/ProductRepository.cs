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
            .Where(p => p.DeletedAt == null && p.Status != ProductStatus.Draft)
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

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
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
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);
    }

    public async Task<IEnumerable<Product>> GetSellerProductsAsync(int sellerId)
    {
        return await _db.Products
            .Include(p => p.Category)
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
}
