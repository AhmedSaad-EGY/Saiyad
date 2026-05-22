using Sayiad.Domain.Dtos.SubscriptionPlanDtos;

namespace Sayiad.Domain.Managers;

public interface ISubscriptionPlanManager
{
    Task<List<SubscriptionPlanResponse>> GetActivePlansAsync();
    Task<SubscriptionPlanResponse> GetByIdAsync(int id);
    Task<SubscriptionPlanResponse> CreateAsync(CreateSubscriptionPlanRequest request);
    Task<SubscriptionPlanResponse> UpdateAsync(int id, UpdateSubscriptionPlanRequest request);
    Task DeleteAsync(int id);
}
