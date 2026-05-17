using Sayiad.Data.Models;

namespace Sayiad.Data.Repository.SubscriptionRepo;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetActiveAsync(int userId);
    Task<List<Subscription>> GetUserSubscriptionsAsync(int userId);
    Task AddAsync(Subscription subscription);
    Task UpdateAsync(Subscription subscription);
    Task<PagedResult<Subscription>> GetAllAsync(PaginationRequest pagination);
    Task<int> GetMonthlyAuctionCountAsync(int userId);
    Task<Dictionary<int, int>> GetMonthlyAuctionCountsAsync(IEnumerable<int> userIds);
}
