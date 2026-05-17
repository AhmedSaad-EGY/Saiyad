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
    private readonly Mock<ILogger<AuctionManager>> _loggerMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private AuctionManager CreateManager() =>
        new(_auctionRepoMock.Object, _productRepoMock.Object,
            _notificationManagerMock.Object, _emailServiceMock.Object,
            _userRepoMock.Object, _unitOfWorkMock.Object, _loggerMock.Object);

    [Fact]
    public async Task CreateAuction_UnderMonthlyLimit_Succeeds()
    {
        var user = new User { Id = 1, SubscriptionTier = SubscriptionTier.Basic };
        var product = new Product { Id = 1, SellerId = 1 };

        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _auctionRepoMock.Setup(r => r.GetUserMonthlyAuctionCountAsync(1)).ReturnsAsync(3);
        _productRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
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

        var manager = CreateManager();
        var request = new CreateAuctionRequest(1, DateTime.UtcNow.AddDays(7), 100, 50, 10);

        var act = () => manager.CreateAsync(1, request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*monthly auction limit*");
    }
}
