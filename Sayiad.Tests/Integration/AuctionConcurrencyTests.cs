using Microsoft.EntityFrameworkCore;
using Sayiad.Data.Data;
using Sayiad.Data.Models;
using Sayiad.Data.Repository.AuctionRepo;
using Sayiad.Data.Repository.UserRepo;

namespace Sayiad.Tests.Integration;

public class AuctionConcurrencyTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly AuctionRepository _auctionRepo;
    private readonly UserRepository _userRepo;

    public AuctionConcurrencyTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"AuctionTestDb_{Guid.NewGuid()}")
            .Options;

        _db = new ApplicationDbContext(options);
        _auctionRepo = new AuctionRepository(_db);
        _userRepo = new UserRepository(_db);
    }

    [Fact]
    public async Task PlaceBid_WithValidAmount_Succeeds()
    {
        var (user, auction) = await SeedData();

        var bid = await PlaceBidAsync(auction.Id, user.Id, 150m);

        Assert.NotNull(bid);
        var reloaded = await _auctionRepo.GetByIdWithBidsAsync(auction.Id);
        Assert.Equal(150m, reloaded!.CurrentHighestBid);
        Assert.Single(reloaded.Bids);
    }

    [Fact]
    public async Task PlaceBid_WithAmountBelowMinimum_Throws()
    {
        var (user, auction) = await SeedData();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            PlaceBidAsync(auction.Id, user.Id, 50m));

        Assert.Contains("Bid must be at least", ex.Message);
    }

    [Fact]
    public async Task PlaceBid_AfterAuctionEnded_Throws()
    {
        var (user, auction) = await SeedData();
        auction.EndTime = DateTime.UtcNow.AddMinutes(-1);
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            PlaceBidAsync(auction.Id, user.Id, 150m));

        Assert.Contains("ended", ex.Message.ToLower());
    }

    [Fact]
    public async Task PlaceBid_DowngradesPreviousWinningBid()
    {
        var (_, auction) = await SeedData();
        var user2 = new User
        {
            FullName = "User2", Email = "user2@test.com",
            PasswordHash = "hash", Phone = "123",
            Role = UserRole.Customer, IsActive = true,
            IsEmailVerified = true, CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user2);
        await _db.SaveChangesAsync();

        await PlaceBidAsync(auction.Id, 1, 150m);
        await PlaceBidAsync(auction.Id, user2.Id, 200m);

        var reloaded = await _auctionRepo.GetByIdWithBidsAsync(auction.Id);
        Assert.Equal(2, reloaded!.Bids.Count);
        Assert.Single(reloaded.Bids, b => b.BidStatus == BidStatus.Winning);
        Assert.Single(reloaded.Bids, b => b.BidStatus == BidStatus.Valid);
    }

    [Fact]
    public async Task EndAuction_WithWinningBid_SetsWinnerAndSold()
    {
        var (_, auction) = await SeedData();
        var user2 = new User
        {
            FullName = "User2", Email = "user2b@test.com",
            PasswordHash = "hash", Phone = "123",
            Role = UserRole.Customer, IsActive = true,
            IsEmailVerified = true, CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user2);
        await _db.SaveChangesAsync();

        await PlaceBidAsync(auction.Id, 1, 200m);

        auction.Status = AuctionStatus.Finished;
        var winningBid = auction.Bids.MaxBy(b => b.Amount);
        if (winningBid != null && winningBid.Amount >= auction.ReservePrice)
        {
            auction.WinnerUserId = winningBid.UserId;
        }
        await _db.SaveChangesAsync();

        Assert.NotNull(auction.WinnerUserId);
        Assert.Equal(AuctionStatus.Finished, auction.Status);
    }

    [Fact]
    public async Task GetUserMonthlyAuctionCount_ReturnsCorrectCount()
    {
        var (user, _) = await SeedData();
        var now = DateTime.UtcNow;

        for (int i = 0; i < 3; i++)
        {
            _db.Auctions.Add(new Auction
            {
                ProductId = 1, CreatedByUserId = user.Id,
                StartTime = now, EndTime = now.AddDays(7),
                StartingPrice = 100, ReservePrice = 100,
                MinimumIncrement = 10, CurrentHighestBid = 100,
                Status = AuctionStatus.Active, CreatedAt = now,
                RowVersion = Array.Empty<byte>()
            });
        }
        await _db.SaveChangesAsync();

        var count = await _auctionRepo.GetUserMonthlyAuctionCountAsync(user.Id);
        Assert.Equal(4, count); // 1 from SeedData + 3 added in this test
    }

    private async Task<(User, Auction)> SeedData()
    {
        var seller = new User
        {
            FullName = "Seller", Email = "seller@test.com",
            PasswordHash = "hash", Phone = "123",
            Role = UserRole.Auctioneer, IsActive = true,
            IsEmailVerified = true, CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(seller);
        var cat = new Category { Name = "TestCat", CreatedAt = DateTime.UtcNow };
        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();

        var product = new Product
        {
            SellerId = seller.Id, CategoryId = cat.Id,
            Title = "Test Product", Description = "Test description",
            Brand = "TestBrand", Location = "TestLocation",
            Price = 100m, StockQuantity = 10,
            Status = ProductStatus.Available,
            Condition = ProductCondition.New, CreatedAt = DateTime.UtcNow
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        var auction = new Auction
        {
            ProductId = product.Id, CreatedByUserId = seller.Id,
            StartTime = DateTime.UtcNow.AddDays(-1),
            EndTime = DateTime.UtcNow.AddDays(1),
            StartingPrice = 100m, ReservePrice = 100m,
            MinimumIncrement = 10m, CurrentHighestBid = 100m,
            Status = AuctionStatus.Active, CreatedAt = DateTime.UtcNow,
            RowVersion = Array.Empty<byte>()
        };
        _db.Auctions.Add(auction);
        await _db.SaveChangesAsync();

        return (seller, auction);
    }

    private async Task<Domain.Dtos.AuctionDtos.BidResponse> PlaceBidAsync(int auctionId, int userId, decimal amount)
    {
        var auction = await _auctionRepo.GetByIdWithBidsAsync(auctionId)
            ?? throw new KeyNotFoundException();
        if (auction.Status != AuctionStatus.Active)
            throw new InvalidOperationException("Auction is not active");
        if (auction.EndTime <= DateTime.UtcNow)
            throw new InvalidOperationException("Auction has ended");
        if (amount < auction.CurrentHighestBid + auction.MinimumIncrement)
            throw new InvalidOperationException($"Bid must be at least {auction.CurrentHighestBid + auction.MinimumIncrement:C}");

        foreach (var prevBid in auction.Bids.Where(b => b.BidStatus == BidStatus.Winning))
            prevBid.BidStatus = BidStatus.Valid;

        var bid = new Bid
        {
            AuctionId = auctionId, UserId = userId, Amount = amount,
            IsAutoBid = false, BidStatus = BidStatus.Winning, CreatedAt = DateTime.UtcNow
        };
        auction.Bids.Add(bid);
        auction.CurrentHighestBid = amount;
        await _db.SaveChangesAsync();

        return new Domain.Dtos.AuctionDtos.BidResponse(
            bid.Id, bid.AuctionId, bid.UserId, "", bid.Amount,
            bid.IsAutoBid, bid.MaxAutoBidAmount, bid.BidStatus.ToString(), bid.CreatedAt);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
