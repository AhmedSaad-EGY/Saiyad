using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
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

    [Fact]
    public async Task ApproveRequest_AtMonthlyLimit_RollsBackAndDoesNotCreateProduct()
    {
        var txMock = new Mock<IDbContextTransaction>();
        txMock.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        txMock.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        txMock.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);

        var auctionRequest = new AuctionRequest
        {
            Id = 50,
            FishermanId = 7,
            ProductTitle = "Sea Bass",
            ProductDescription = "Fresh catch",
            EstimatedValue = 100,
            QuantityKg = 2,
            FishType = "Bass",
            CatchLocation = "Alexandria",
            Status = AuctionRequestStatus.Pending
        };
        var auctioneer = new User { Id = 9, SubscriptionTier = SubscriptionTier.Free };

        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(txMock.Object);
        _auctionRepoMock.Setup(r => r.GetRequestByIdAsync(50)).ReturnsAsync(auctionRequest);
        _userRepoMock.Setup(r => r.GetByIdAsync(9)).ReturnsAsync(auctioneer);
        _planRepoMock.Setup(r => r.GetByTierAsync(SubscriptionTier.Free)).ReturnsAsync(FreePlan);
        _auctionRepoMock.Setup(r => r.GetUserMonthlyAuctionCountAsync(9)).ReturnsAsync(3);

        var manager = CreateManager();
        var request = new ApproveAuctionRequestRequest(DateTime.UtcNow.AddDays(7), 100, 80, 10, 1);

        var act = () => manager.ApproveRequestAsync(50, 9, request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Monthly auction limit*");

        _productRepoMock.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Never);
        _auctionRepoMock.Verify(r => r.AddAsync(It.IsAny<Auction>()), Times.Never);
        _auctionRepoMock.Verify(r => r.UpdateRequestAsync(It.IsAny<AuctionRequest>()), Times.Never);
        txMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        txMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApproveRequest_Success_CommitsProductAuctionAndRequestUpdate()
    {
        var txMock = new Mock<IDbContextTransaction>();
        txMock.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        txMock.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        txMock.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);

        var auctionRequest = new AuctionRequest
        {
            Id = 51,
            FishermanId = 7,
            ProductTitle = "Sea Bass",
            ProductDescription = "Fresh catch",
            ProductImageUrl = "https://example.test/fish.jpg",
            EstimatedValue = 100,
            QuantityKg = 2,
            FishType = "Bass",
            CatchLocation = "Alexandria",
            Status = AuctionRequestStatus.Pending
        };
        var auctioneer = new User { Id = 9, SubscriptionTier = SubscriptionTier.Basic };
        Product? createdProduct = null;

        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(txMock.Object);
        _auctionRepoMock.Setup(r => r.GetRequestByIdAsync(51)).ReturnsAsync(auctionRequest);
        _userRepoMock.Setup(r => r.GetByIdAsync(9)).ReturnsAsync(auctioneer);
        _planRepoMock.Setup(r => r.GetByTierAsync(SubscriptionTier.Basic)).ReturnsAsync(BasicPlan);
        _auctionRepoMock.Setup(r => r.GetUserMonthlyAuctionCountAsync(9)).ReturnsAsync(0);
        _productRepoMock.Setup(r => r.AddAsync(It.IsAny<Product>()))
            .Callback<Product>(p =>
            {
                p.Id = 22;
                createdProduct = p;
            })
            .Returns(Task.CompletedTask);
        _productRepoMock.Setup(r => r.GetByIdAsync(22))
            .ReturnsAsync(() => createdProduct);
        _auctionRepoMock.Setup(r => r.AddAsync(It.IsAny<Auction>()))
            .Returns(Task.CompletedTask)
            .Callback<Auction>(a => a.Id = 33);
        _auctionRepoMock.Setup(r => r.GetByIdWithDetailsAsync(33))
            .ReturnsAsync(new Auction
            {
                Id = 33,
                ProductId = 22,
                Product = new Product { Id = 22, Title = "Sea Bass", Images = new List<ProductImage>() },
                Bids = new List<Bid>(),
                Status = AuctionStatus.Active
            });
        _auctionRepoMock.Setup(r => r.UpdateRequestAsync(It.IsAny<AuctionRequest>()))
            .ReturnsAsync((AuctionRequest r) => r);
        _notificationManagerMock.Setup(n => n.CreateAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var manager = CreateManager();
        var request = new ApproveAuctionRequestRequest(DateTime.UtcNow.AddDays(7), 100, 80, 10, 1);

        var result = await manager.ApproveRequestAsync(51, 9, request);

        result.Id.Should().Be(33);
        auctionRequest.Status.Should().Be(AuctionRequestStatus.Approved);
        auctionRequest.ResultingAuctionId.Should().Be(33);
        _productRepoMock.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Once);
        _auctionRepoMock.Verify(r => r.AddAsync(It.IsAny<Auction>()), Times.Once);
        _auctionRepoMock.Verify(r => r.UpdateRequestAsync(auctionRequest), Times.Once);
        _notificationManagerMock.Verify(n => n.CreateAsync(
            7,
            "Auction Request Approved",
            It.Is<string>(m => m.Contains("Auction #33"))), Times.Once);
        txMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        txMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
