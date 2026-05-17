using Sayiad.Domain.Dtos.Subscription;

namespace Sayiad.Domain.Contracts.Subscription;

public interface ISubscriptionManager
{
    Task<Result<SubscriptionResponse>> UpgradeAsync(int userId, UpgradeSubscriptionRequest request);
    Task<Result<SubscriptionResponse>> GetMySubscriptionAsync(int userId);
    Task<Result<PagedResult<SubscriptionResponse>>> GetAllAsync(PaginationRequest pagination);
}
