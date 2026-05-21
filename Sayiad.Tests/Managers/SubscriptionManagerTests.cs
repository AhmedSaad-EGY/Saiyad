using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Sayiad.Tests.Managers;

public class SubscriptionManagerTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<ISubscriptionRepository> _subRepoMock = new();
    private readonly Mock<ILogger<SubscriptionManager>> _loggerMock = new();

    private SubscriptionManager CreateManager() =>
        new(_userRepoMock.Object, _subRepoMock.Object, _loggerMock.Object);

    [Fact]
    public async Task UpgradeAsync_WithValidTier_CreatesSubscription()
    {
        var user = new User { Id = 1, SubscriptionTier = SubscriptionTier.Free };
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _subRepoMock.Setup(r => r.GetActiveAsync(1)).ReturnsAsync((Subscription?)null);
        _subRepoMock.Setup(r => r.GetMonthlyAuctionCountAsync(1)).ReturnsAsync(0);
        _subRepoMock.Setup(r => r.PaymentReferenceExistsAsync("pay_123")).ReturnsAsync(false);

        var manager = CreateManager();
        var result = await manager.UpgradeAsync(1,
            new UpgradeSubscriptionRequest("Pro", "pay_123"));

        result.IsSuccess.Should().BeTrue();
        result.Data!.Tier.Should().Be("Pro");
        result.Data.AuctionsPerMonth.Should().Be(25);
        result.Data.PaymentReference.Should().Be("pay_123");
        user.SubscriptionTier.Should().Be(SubscriptionTier.Pro);
    }

    [Fact]
    public async Task UpgradeAsync_WithInvalidTier_ReturnsFailure()
    {
        var user = new User { Id = 1 };
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        var manager = CreateManager();
        var result = await manager.UpgradeAsync(1,
            new UpgradeSubscriptionRequest("Ultra", "pay_123"));

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpgradeAsync_WithDuplicatePaymentReference_ReturnsFailure()
    {
        var user = new User { Id = 1, SubscriptionTier = SubscriptionTier.Free };
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _subRepoMock.Setup(r => r.PaymentReferenceExistsAsync("pay_dup")).ReturnsAsync(true);

        var manager = CreateManager();
        var result = await manager.UpgradeAsync(1,
            new UpgradeSubscriptionRequest("Pro", "pay_dup"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Duplicate");
    }

    [Fact]
    public async Task GetMySubscription_WithNoActiveSub_ReturnsFreeTier()
    {
        var user = new User { Id = 1, SubscriptionTier = SubscriptionTier.Free };
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _subRepoMock.Setup(r => r.GetActiveAsync(1)).ReturnsAsync((Subscription?)null);
        _subRepoMock.Setup(r => r.GetMonthlyAuctionCountAsync(1)).ReturnsAsync(2);

        var manager = CreateManager();
        var result = await manager.GetMySubscriptionAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Tier.Should().Be("Free");
        result.Data.AuctionsPerMonth.Should().Be(3);
        result.Data.AuctionsUsedThisMonth.Should().Be(2);
        result.Data.AuctionsRemaining.Should().Be(1);
    }
}
