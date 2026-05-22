using Microsoft.Extensions.Logging;
using Sayiad.Domain.Dtos.SubscriptionPlanDtos;

namespace Sayiad.Domain.Managers;

public class SubscriptionPlanManager : ISubscriptionPlanManager
{
    private readonly ISubscriptionPlanRepository _planRepo;
    private readonly ILogger<SubscriptionPlanManager> _logger;

    public SubscriptionPlanManager(ISubscriptionPlanRepository planRepo, ILogger<SubscriptionPlanManager> logger)
    {
        _planRepo = planRepo;
        _logger = logger;
    }

    public async Task<List<SubscriptionPlanResponse>> GetActivePlansAsync()
    {
        var plans = await _planRepo.GetActivePlansAsync();
        return plans.Select(MapToResponse).ToList();
    }

    public async Task<SubscriptionPlanResponse> GetByIdAsync(int id)
    {
        var plan = await _planRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Subscription plan not found");
        return MapToResponse(plan);
    }

    public async Task<SubscriptionPlanResponse> CreateAsync(CreateSubscriptionPlanRequest request)
    {
        if (!Enum.TryParse<SubscriptionTier>(request.Tier, out var tier))
            throw new InvalidOperationException("Invalid tier. Must be: Free, Basic, Pro, or Enterprise.");

        var existing = await _planRepo.GetByTierAsync(tier);
        if (existing != null)
            throw new InvalidOperationException("A plan for this tier already exists.");

        var plan = new SubscriptionPlan
        {
            Tier = tier,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Currency = request.Currency,
            BillingCycle = request.BillingCycle,
            MaxAuctionsPerMonth = request.MaxAuctionsPerMonth,
            MaxBidsPerMonth = request.MaxBidsPerMonth,
            MaxAuctionRequestsPerMonth = request.MaxAuctionRequestsPerMonth,
            Features = System.Text.Json.JsonSerializer.Serialize(request.Features),
            SortOrder = request.SortOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _planRepo.CreateAsync(plan);
        _logger.LogInformation("Subscription plan created: {Name} ({Tier})", request.Name, request.Tier);
        return MapToResponse(created);
    }

    public async Task<SubscriptionPlanResponse> UpdateAsync(int id, UpdateSubscriptionPlanRequest request)
    {
        var plan = await _planRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Subscription plan not found");

        if (request.Name != null) plan.Name = request.Name;
        if (request.Description != null) plan.Description = request.Description;
        if (request.Price.HasValue) plan.Price = request.Price.Value;
        if (request.Currency != null) plan.Currency = request.Currency;
        if (request.BillingCycle != null) plan.BillingCycle = request.BillingCycle;
        if (request.MaxAuctionsPerMonth.HasValue) plan.MaxAuctionsPerMonth = request.MaxAuctionsPerMonth.Value;
        if (request.MaxBidsPerMonth.HasValue) plan.MaxBidsPerMonth = request.MaxBidsPerMonth.Value;
        if (request.MaxAuctionRequestsPerMonth.HasValue) plan.MaxAuctionRequestsPerMonth = request.MaxAuctionRequestsPerMonth.Value;
        if (request.Features != null) plan.Features = System.Text.Json.JsonSerializer.Serialize(request.Features);
        if (request.IsActive.HasValue) plan.IsActive = request.IsActive.Value;
        if (request.SortOrder.HasValue) plan.SortOrder = request.SortOrder.Value;

        var updated = await _planRepo.UpdateAsync(plan);
        _logger.LogInformation("Subscription plan updated: {Name} ({Id})", plan.Name, id);
        return MapToResponse(updated);
    }

    public async Task DeleteAsync(int id)
    {
        var plan = await _planRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Subscription plan not found");
        await _planRepo.DeleteAsync(plan);
        _logger.LogInformation("Subscription plan deleted: {Id}", id);
    }

    private static SubscriptionPlanResponse MapToResponse(SubscriptionPlan plan)
    {
        var features = System.Text.Json.JsonSerializer.Deserialize<string[]>(plan.Features) ?? [];
        return new SubscriptionPlanResponse(
            plan.Id, plan.Tier.ToString(), plan.Name, plan.Description,
            plan.Price, plan.Currency, plan.BillingCycle,
            plan.MaxAuctionsPerMonth, plan.MaxBidsPerMonth, plan.MaxAuctionRequestsPerMonth,
            features, plan.IsActive, plan.SortOrder
        );
    }
}
