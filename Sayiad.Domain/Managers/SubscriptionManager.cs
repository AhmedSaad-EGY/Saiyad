using Microsoft.Extensions.Logging;
using Sayiad.Domain.Contracts.Subscription;
using Sayiad.Domain.Dtos.Subscription;

namespace Sayiad.Domain.Managers;

public class SubscriptionManager : ISubscriptionManager
{
    private readonly IUserRepository _userRepo;
    private readonly ISubscriptionRepository _subRepo;
    private readonly ISubscriptionPlanRepository _planRepo;
    private readonly IWalletManager _walletManager;
    private readonly ILogger<SubscriptionManager> _logger;

    public SubscriptionManager(
        IUserRepository userRepo,
        ISubscriptionRepository subRepo,
        ISubscriptionPlanRepository planRepo,
        IWalletManager walletManager,
        ILogger<SubscriptionManager> logger)
    {
        _userRepo = userRepo;
        _subRepo = subRepo;
        _planRepo = planRepo;
        _walletManager = walletManager;
        _logger = logger;
    }

    public async Task<Result<SubscriptionResponse>> UpgradeAsync(int userId, UpgradeSubscriptionRequest request)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user is null)
            return Result<SubscriptionResponse>.Failure("User not found.");

        if (!Enum.TryParse<SubscriptionTier>(request.Tier, out var tier))
            return Result<SubscriptionResponse>.Failure("Invalid subscription tier. Must be one of: Basic, Pro, Enterprise.");

        var plan = await _planRepo.GetByTierAsync(tier);
        if (plan == null)
            return Result<SubscriptionResponse>.Failure("Subscription plan not found for this tier.");

        if (plan.Price > 0 && !await _walletManager.HasSufficientBalanceAsync(userId, plan.Price))
            return Result<SubscriptionResponse>.Failure("Insufficient wallet balance. Please deposit funds to upgrade.");

        var duplicateRef = await _subRepo.PaymentReferenceExistsAsync(request.PaymentReference);
        if (duplicateRef)
            return Result<SubscriptionResponse>.Failure("Payment reference already exists. Duplicate payment references are not allowed.");

        var activeSub = await _subRepo.GetActiveAsync(userId);
        if (activeSub is not null)
        {
            activeSub.IsActive = false;
            activeSub.EndDate = DateTime.UtcNow;
            await _subRepo.UpdateAsync(activeSub);
        }

        var subscription = new Subscription
        {
            UserId = userId,
            Tier = tier,
            StartDate = DateTime.UtcNow,
            IsActive = true,
            PaymentReference = request.PaymentReference
        };

        await _subRepo.AddAsync(subscription);

        if (plan.Price > 0)
        {
            await _walletManager.DeductForSubscriptionAsync(userId, plan.Price, subscription.Id);

            var admin = await _userRepo.GetByEmailAsync("sayiadapp@gmail.com");
            if (admin != null)
            {
                await _walletManager.CreditPlatformFeeAsync(admin.Id, plan.Price, "Subscription", subscription.Id);
            }
        }

        user.SubscriptionTier = tier;
        await _userRepo.UpdateAsync(user);

        var used = await _subRepo.GetMonthlyAuctionCountAsync(userId);

        _logger.LogInformation("Subscription upgraded: User {UserId} to {Tier}", userId, request.Tier);

        return Result<SubscriptionResponse>.Success(MapToResponse(subscription, plan, used));
    }

    public async Task<Result<SubscriptionResponse>> GetMySubscriptionAsync(int userId)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user is null)
            return Result<SubscriptionResponse>.Failure("User not found.");

        var activeSub = await _subRepo.GetActiveAsync(userId);
        var used = await _subRepo.GetMonthlyAuctionCountAsync(userId);

        var plan = await _planRepo.GetByTierAsync(activeSub?.Tier ?? SubscriptionTier.Free);
        if (plan == null)
            return Result<SubscriptionResponse>.Failure("No subscription plan configured for your tier.");

        if (activeSub is null)
        {
            return Result<SubscriptionResponse>.Success(new SubscriptionResponse(
                0, "Free", plan.Price, plan.MaxAuctionsPerMonth,
                used, plan.MaxAuctionsPerMonth - used,
                DateTime.UtcNow, null, true, null, null
            ));
        }

        return Result<SubscriptionResponse>.Success(MapToResponse(activeSub, plan, used));
    }

    public async Task<Result<PagedResult<SubscriptionResponse>>> GetAllAsync(PaginationRequest pagination)
    {
        var result = await _subRepo.GetAllAsync(pagination);

        var userIds = result.Items.Select(s => s.UserId).Distinct();
        var counts = await _subRepo.GetMonthlyAuctionCountsAsync(userIds);

        var items = new List<SubscriptionResponse>();
        foreach (var sub in result.Items)
        {
            var plan = await _planRepo.GetByTierAsync(sub.Tier);
            var used = counts.GetValueOrDefault(sub.UserId, 0);
            items.Add(MapToResponse(sub, plan ?? new SubscriptionPlan { Price = 0, MaxAuctionsPerMonth = 3 }, used));
        }

        return Result<PagedResult<SubscriptionResponse>>.Success(new PagedResult<SubscriptionResponse>
        {
            Items = items,
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        });
    }

    private static SubscriptionResponse MapToResponse(Subscription sub, SubscriptionPlan plan, int used)
    {
        return new SubscriptionResponse(
            sub.Id,
            sub.Tier.ToString(),
            plan.Price,
            plan.MaxAuctionsPerMonth,
            used,
            plan.MaxAuctionsPerMonth - used,
            sub.StartDate,
            sub.EndDate,
            sub.IsActive,
            sub.IsActive ? sub.StartDate.AddMonths(1) : null,
            sub.PaymentReference
        );
    }

    public static async Task<int> GetMonthlyLimitAsync(ISubscriptionPlanRepository planRepo, SubscriptionTier tier)
    {
        var plan = await planRepo.GetByTierAsync(tier);
        return plan?.MaxAuctionsPerMonth ?? 3;
    }
}
