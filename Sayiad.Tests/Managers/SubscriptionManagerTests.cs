using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Sayiad.Data.Data;
using Sayiad.Domain.Common;

namespace Sayiad.Tests.Managers;

public class SubscriptionManagerTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<ISubscriptionRepository> _subRepoMock = new();
    private readonly Mock<ISubscriptionPlanRepository> _planRepoMock = new();
    private readonly Mock<IWalletManager> _walletManagerMock = new();
    private readonly Mock<ILogger<SubscriptionManager>> _loggerMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IOptions<AppSettings>> _settingsMock = new();
    private readonly Mock<IDbContextTransaction> _txMock = new();

    private static readonly SubscriptionPlan ProPlan = new()
    {
        Id = 1, Tier = SubscriptionTier.Pro, Name = "Pro", Price = 20,
        MaxAuctionsPerMonth = 25, MaxBidsPerMonth = 50, MaxAuctionRequestsPerMonth = 25,
        Features = "[]", IsActive = true, SortOrder = 3
    };

    private static readonly SubscriptionPlan FreePlan = new()
    {
        Id = 0, Tier = SubscriptionTier.Free, Name = "Free", Price = 0,
        MaxAuctionsPerMonth = 3, MaxBidsPerMonth = 3, MaxAuctionRequestsPerMonth = 3,
        Features = "[]", IsActive = true, SortOrder = 1
    };

    private SubscriptionManager CreateManager()
    {
        _txMock.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_txMock.Object);
        _settingsMock.Setup(s => s.Value).Returns(new AppSettings { AdminEmail = "admin@test.com" });
        return new(_userRepoMock.Object, _subRepoMock.Object, _planRepoMock.Object, _walletManagerMock.Object,
            _unitOfWorkMock.Object, _settingsMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task UpgradeAsync_WithValidTier_CreatesSubscription()
    {
        var user = new User { Id = 1, SubscriptionTier = SubscriptionTier.Free };
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _subRepoMock.Setup(r => r.GetActiveAsync(1)).ReturnsAsync((Subscription?)null);
        _subRepoMock.Setup(r => r.GetMonthlyAuctionCountAsync(1)).ReturnsAsync(0);
        _subRepoMock.Setup(r => r.PaymentReferenceExistsAsync("pay_123")).ReturnsAsync(false);
        _planRepoMock.Setup(r => r.GetByTierAsync(SubscriptionTier.Pro)).ReturnsAsync(ProPlan);
        _walletManagerMock.Setup(w => w.HasSufficientBalanceAsync(1, ProPlan.Price)).ReturnsAsync(true);

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
        _planRepoMock.Setup(r => r.GetByTierAsync(SubscriptionTier.Pro)).ReturnsAsync(ProPlan);
        _walletManagerMock.Setup(w => w.HasSufficientBalanceAsync(1, ProPlan.Price)).ReturnsAsync(true);

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
        _planRepoMock.Setup(r => r.GetByTierAsync(SubscriptionTier.Free)).ReturnsAsync(FreePlan);

        var manager = CreateManager();
        var result = await manager.GetMySubscriptionAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Tier.Should().Be("Free");
        result.Data.AuctionsPerMonth.Should().Be(3);
        result.Data.AuctionsUsedThisMonth.Should().Be(2);
        result.Data.AuctionsRemaining.Should().Be(1);
    }
}
