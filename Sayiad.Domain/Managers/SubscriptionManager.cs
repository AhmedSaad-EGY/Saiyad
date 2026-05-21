using Microsoft.Extensions.Logging;
using Sayiad.Domain.Contracts.Subscription;
using Sayiad.Domain.Dtos.Subscription;

namespace Sayiad.Domain.Managers;

public class SubscriptionManager : ISubscriptionManager
{
    private readonly IUserRepository _userRepo;
    private readonly ISubscriptionRepository _subRepo;
    private readonly ILogger<SubscriptionManager> _logger;

    private static readonly Dictionary<SubscriptionTier, (decimal Price, int AuctionsPerMonth)> TierLimits = new()
    {
        [SubscriptionTier.Free] = (0, 3),
        [SubscriptionTier.Basic] = (10, 10),
        [SubscriptionTier.Pro] = (20, 25),
        [SubscriptionTier.Enterprise] = (50, 100)
    };

    public SubscriptionManager(
        IUserRepository userRepo,
        ISubscriptionRepository subRepo,
        ILogger<SubscriptionManager> logger)
    {
        _userRepo = userRepo;
        _subRepo = subRepo;
        _logger = logger;
    }

    public async Task<Result<SubscriptionResponse>> UpgradeAsync(int userId, UpgradeSubscriptionRequest request)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user is null)
            return Result<SubscriptionResponse>.Failure("User not found.");

        if (!Enum.TryParse<SubscriptionTier>(request.Tier, out var tier))
            return Result<SubscriptionResponse>.Failure("Invalid subscription tier. Must be one of: Basic, Pro, Enterprise.");

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

        user.SubscriptionTier = tier;
        await _userRepo.UpdateAsync(user);

        var used = await _subRepo.GetMonthlyAuctionCountAsync(userId);
        var limits = TierLimits[tier];

        _logger.LogInformation("Subscription upgraded: User {UserId} to {Tier}", userId, request.Tier);

        return Result<SubscriptionResponse>.Success(MapToResponse(subscription, user, used));
    }

    public async Task<Result<SubscriptionResponse>> GetMySubscriptionAsync(int userId)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user is null)
            return Result<SubscriptionResponse>.Failure("User not found.");

        var activeSub = await _subRepo.GetActiveAsync(userId);
        var used = await _subRepo.GetMonthlyAuctionCountAsync(userId);

        if (activeSub is null)
        {
            var limits = TierLimits[SubscriptionTier.Free];
            return Result<SubscriptionResponse>.Success(new SubscriptionResponse(
                0, "Free", limits.Price, limits.AuctionsPerMonth,
                used, limits.AuctionsPerMonth - used,
                DateTime.UtcNow, null, true, null, null
            ));
        }

        return Result<SubscriptionResponse>.Success(MapToResponse(activeSub, user, used));
    }

    public async Task<Result<PagedResult<SubscriptionResponse>>> GetAllAsync(PaginationRequest pagination)
    {
        var result = await _subRepo.GetAllAsync(pagination);

        var userIds = result.Items.Select(s => s.UserId).Distinct();
        var counts = await _subRepo.GetMonthlyAuctionCountsAsync(userIds);

        var items = result.Items.Select(sub =>
        {
            var used = counts.GetValueOrDefault(sub.UserId, 0);
            return MapToResponse(sub, sub.User, used);
        }).ToList();

        return Result<PagedResult<SubscriptionResponse>>.Success(new PagedResult<SubscriptionResponse>
        {
            Items = items,
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        });
    }

    private static SubscriptionResponse MapToResponse(Subscription sub, User user, int used)
    {
        var limits = TierLimits[sub.Tier];
        return new SubscriptionResponse(
            sub.Id,
            sub.Tier.ToString(),
            limits.Price,
            limits.AuctionsPerMonth,
            used,
            limits.AuctionsPerMonth - used,
            sub.StartDate,
            sub.EndDate,
            sub.IsActive,
            sub.IsActive ? sub.StartDate.AddMonths(1) : null,
            sub.PaymentReference
        );
    }

    private static SubscriptionResponse MapToResponse(User user, int used)
    {
        var limits = TierLimits[user.SubscriptionTier];
        return new SubscriptionResponse(
            0,
            user.SubscriptionTier.ToString(),
            limits.Price,
            limits.AuctionsPerMonth,
            used,
            limits.AuctionsPerMonth - used,
            DateTime.UtcNow,
            null,
            true,
            null,
            null
        );
    }

    public static int GetMonthlyLimit(SubscriptionTier tier) => TierLimits[tier].AuctionsPerMonth;
}
