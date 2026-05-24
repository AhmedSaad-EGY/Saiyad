using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Sayiad.Data.Data;

namespace Sayiad.Tests.Managers;

public class AuctionQuotaTests
{
    private readonly Mock<IAuctionRepository> _auctionRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<INotificationManager> _notificationManagerMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<ISubscriptionPlanRepository> _planRepoMock = new();
    private readonly Mock<ILogger<AuctionManager>> _loggerMock = new();
    private readonly Mock<IWalletManager> _walletManagerMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private static readonly SubscriptionPlan BasicPlan = new()
    {
        Id = 2, Tier = SubscriptionTier.Basic, Name = "Basic", Price = 10,
        MaxAuctionsPerMonth = 10, MaxBidsPerMonth = 20, MaxAuctionRequestsPerMonth = 10,
        Features = "[]", IsActive = true, SortOrder = 2
    };

    private static readonly SubscriptionPlan FreePlan = new()
    {
        Id = 1, Tier = SubscriptionTier.Free, Name = "Free", Price = 0,
        MaxAuctionsPerMonth = 3, MaxBidsPerMonth = 3, MaxAuctionRequestsPerMonth = 3,
        Features = "[]", IsActive = true, SortOrder = 1
    };

    private AuctionManager CreateManager() =>
        new(_auctionRepoMock.Object, _productRepoMock.Object,
            _notificationManagerMock.Object, _emailServiceMock.Object,
            _userRepoMock.Object, _planRepoMock.Object,
            _unitOfWorkMock.Object, _loggerMock.Object, _walletManagerMock.Object);

    [Fact]
    public async Task CreateAuction_UnderMonthlyLimit_Succeeds()
    {
        var user = new User { Id = 1, SubscriptionTier = SubscriptionTier.Basic };
        var product = new Product { Id = 1, SellerId = 1 };

        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _auctionRepoMock.Setup(r => r.GetUserMonthlyAuctionCountAsync(1)).ReturnsAsync(3);
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _planRepoMock.Setup(r => r.GetByTierAsync(SubscriptionTier.Basic)).ReturnsAsync(BasicPlan);
        _auctionRepoMock.Setup(r => r.AddAsync(It.IsAny<Auction>()))
            .Returns(Task.CompletedTask)
            .Callback<Auction>(a => a.Id = 10);
        _auctionRepoMock.Setup(r => r.GetByIdWithDetailsAsync(10))
            .ReturnsAsync(new Auction
            {
                Id = 10,
                ProductId = 1,
                Product = new Product { Id = 1, Title = "Test", Images = new List<ProductImage>() },
                Bids = new List<Bid>(),
                Status = AuctionStatus.Active
            });

        var manager = CreateManager();
        var request = new CreateAuctionRequest(1, DateTime.UtcNow.AddDays(7), 100, 50, 10);

        var result = await manager.CreateAsync(1, request);

        result.Should().NotBeNull();
        result.ProductTitle.Should().Be("Test");
    }

    [Fact]
    public async Task CreateAuction_AtMonthlyLimit_ThrowsInvalidOperationException()
    {
        var user = new User { Id = 1, SubscriptionTier = SubscriptionTier.Free };

        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _auctionRepoMock.Setup(r => r.GetUserMonthlyAuctionCountAsync(1)).ReturnsAsync(3);
        _planRepoMock.Setup(r => r.GetByTierAsync(SubscriptionTier.Free)).ReturnsAsync(FreePlan);

        var manager = CreateManager();
        var request = new CreateAuctionRequest(1, DateTime.UtcNow.AddDays(7), 100, 50, 10);

        var act = () => manager.CreateAsync(1, request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*monthly auction limit*");
    }
}
