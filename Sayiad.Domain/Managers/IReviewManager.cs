using Sayiad.Domain.Dtos.ReviewDtos;

namespace Sayiad.Domain.Managers;

public interface IReviewManager
{
    Task<IEnumerable<ReviewResponse>> GetProductReviewsAsync(int productId);
    Task<ProductRatingResponse> GetProductRatingAsync(int productId);
    Task<ReviewResponse> CreateAsync(int userId, CreateReviewRequest request);
    Task<ReviewResponse> UpdateAsync(int reviewId, int userId, UpdateReviewRequest request);
    Task DeleteAsync(int reviewId, int userId);
    Task AdminDeleteAsync(int reviewId, string? reason);
}
