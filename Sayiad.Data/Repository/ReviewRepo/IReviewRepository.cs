namespace Sayiad.Data.Repository.ReviewRepo;

public interface IReviewRepository
{
    Task<IEnumerable<Review>> GetProductReviewsAsync(int productId);
    Task<Review?> GetByIdAsync(int id);
    Task<bool> ExistsForUserAsync(int productId, int userId);
    Task AddAsync(Review review);
    Task DeleteAsync(Review review);
}
