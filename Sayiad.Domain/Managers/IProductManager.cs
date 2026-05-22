using Sayiad.Data.Common;
using Sayiad.Data.Repository;
using Sayiad.Domain.Dtos.ProductDtos;

namespace Sayiad.Domain.Managers;

public interface IProductManager
{
    Task<PagedResult<ProductResponse>> GetAllAsync(ProductFilterRequest? filter = null, PaginationRequest? pagination = null);
    Task<ProductResponse> GetByIdAsync(int id);
    Task<ProductResponse> CreateAsync(int sellerId, CreateProductRequest request);
    Task<ProductResponse> UpdateAsync(int id, int sellerId, UpdateProductRequest request);
    Task DeleteAsync(int id, int sellerId);
    Task<IEnumerable<ProductResponse>> GetSellerProductsAsync(int sellerId);
    Task<ProductImageResponse> AddImageAsync(int productId, int sellerId, AddProductImageRequest request);
    Task DeleteImageAsync(int productId, int imageId, int sellerId);
    Task<ProductResponse> UpdateStatusAsync(int id, ProductStatus status);
    Task<PagedResult<ProductResponse>> GetPendingReviewAsync(PaginationRequest pagination);
    Task<ProductResponse> ApproveProductAsync(int id, int adminId);
    Task<ProductResponse> RejectProductAsync(int id, int adminId, string reason);
}
