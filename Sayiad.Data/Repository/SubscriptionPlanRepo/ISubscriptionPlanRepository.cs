namespace Sayiad.Data.Repository.SubscriptionPlanRepo;

public interface ISubscriptionPlanRepository
{
    Task<List<SubscriptionPlan>> GetActivePlansAsync();
    Task<List<SubscriptionPlan>> GetAllPlansAsync();
    Task<SubscriptionPlan?> GetByIdAsync(int id);
    Task<SubscriptionPlan?> GetByTierAsync(SubscriptionTier tier);
    Task<SubscriptionPlan> CreateAsync(SubscriptionPlan plan);
    Task<SubscriptionPlan> UpdateAsync(SubscriptionPlan plan);
    Task DeleteAsync(SubscriptionPlan plan);
    Task<PagedResult<SubscriptionPlan>> GetAllAsync(PaginationRequest pagination);
}
