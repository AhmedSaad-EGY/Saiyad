using Sayiad.Data.Common;

namespace Sayiad.Data.Repository.ProductRepo;

public interface IProductRepository
{
    Task<PagedResult<Product>> GetAllAsync(ProductFilterRequest filter, PaginationRequest pagination);
    Task<Product?> GetByIdAsync(int id);
    Task<IEnumerable<Product>> GetSellerProductsAsync(int sellerId);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task<bool> ExistsAsync(int id);
    Task<ProductImage> AddImageAsync(ProductImage image);
    Task<ProductImage?> GetImageByIdAsync(int imageId);
    Task DeleteImageAsync(ProductImage image);
}
