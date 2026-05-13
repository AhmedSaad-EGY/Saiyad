using Microsoft.EntityFrameworkCore;
using Sayiad.Data.Repository.ReviewRepo;
using Sayiad.Data.Data;

namespace Sayiad.Data.Repository.ReviewRepo;

public class ReviewRepository : IReviewRepository
{
    private readonly ApplicationDbContext _db;

    public ReviewRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Review>> GetProductReviewsAsync(int productId)
    {
        return await _db.Reviews
            .Include(r => r.User)
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Review?> GetByIdAsync(int id)
    {
        return await _db.Reviews
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<bool> ExistsForUserAsync(int productId, int userId)
    {
        return await _db.Reviews.AnyAsync(r => r.ProductId == productId && r.UserId == userId);
    }

    public async Task AddAsync(Review review)
    {
        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Review review)
    {
        _db.Reviews.Remove(review);
        await _db.SaveChangesAsync();
    }
}
