using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using Sayiad.Data.Data;
using Sayiad.Data.Models;
using Sayiad.Data.Repository.SystemWalletRepo;
using Sayiad.Data.Repository.WalletRepo;
using Sayiad.Domain.Constants;
using Sayiad.Domain.Dtos.WalletDtos;
using Sayiad.Domain.Managers;
using Xunit;

namespace Sayiad.Tests.Managers;

public class WalletManagerTests
{
    private readonly Mock<IWalletRepository> _walletRepoMock = new();
    private readonly Mock<ISystemWalletRepository> _systemWalletRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILogger<WalletManager>> _loggerMock = new();
    private readonly Mock<IDbContextTransaction> _txMock = new();

    private const int UserId = 42;
    private const int SellerId = 7;
    private const int PlatformUserId = 1;
    private static readonly DateTime Now = DateTime.UtcNow;

    public WalletManagerTests()
    {
        _txMock.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _txMock.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_txMock.Object);
    }

    private static Wallet CreateWallet(int userId, decimal balance = 500m, decimal held = 0m)
    {
        return new Wallet
        {
            Id = userId * 10,
            UserId = userId,
            Balance = balance,
            HeldBalance = held,
            CreatedAt = Now,
            UpdatedAt = Now,
            Transactions = new List<WalletTransaction>()
        };
    }

    private WalletManager CreateManager() =>
        new(_walletRepoMock.Object, _systemWalletRepoMock.Object, _unitOfWorkMock.Object, _loggerMock.Object);

    // -------------------------------------------------------
    //  GetWalletAsync
    // -------------------------------------------------------

    [Fact]
    public async Task GetWalletAsync_WhenWalletExists_ReturnsWalletResponse()
    {
        var wallet = CreateWallet(UserId, balance: 250m, held: 50m);
        _walletRepoMock.Setup(r => r.GetByUserIdWithLockAsync(UserId)).ReturnsAsync(wallet);

        var result = await CreateManager().GetWalletAsync(UserId);

        result.Should().NotBeNull();
        result.Balance.Should().Be(250m);
        result.HeldBalance.Should().Be(50m);
        result.AvailableBalance.Should().Be(200m);
        result.CreatedAt.Should().Be(Now);
        _walletRepoMock.Verify(r => r.GetByUserIdWithLockAsync(UserId), Times.Once);
        _walletRepoMock.Verify(r => r.CreateAsync(It.IsAny<Wallet>()), Times.Never);
    }

    [Fact]
    public async Task GetWalletAsync_WhenWalletMissing_CreatesAndReturnsWalletResponse()
    {
        var createdWallet = CreateWallet(UserId, balance: 0m, held: 0m);
        _walletRepoMock.SetupSequence(r => r.GetByUserIdWithLockAsync(UserId))
            .ReturnsAsync((Wallet?)null)
            .ReturnsAsync(createdWallet);
        _walletRepoMock.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync((Wallet?)null);
        _walletRepoMock.Setup(r => r.CreateAsync(It.IsAny<Wallet>())).ReturnsAsync((Wallet)null!);

        var result = await CreateManager().GetWalletAsync(UserId);

        result.Should().NotBeNull();
        result.Balance.Should().Be(0m);
        result.HeldBalance.Should().Be(0m);
        result.AvailableBalance.Should().Be(0m);
        _walletRepoMock.Verify(r => r.CreateAsync(It.Is<Wallet>(w => w.UserId == UserId)), Times.Once);
    }

    [Fact]
    public async Task GetWalletAsync_WhenCreationFails_ThrowsKeyNotFoundException()
    {
        _walletRepoMock.SetupSequence(r => r.GetByUserIdWithLockAsync(UserId))
            .ReturnsAsync((Wallet?)null)
            .ReturnsAsync((Wallet?)null);
        _walletRepoMock.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync((Wallet?)null);
        _walletRepoMock.Setup(r => r.CreateAsync(It.IsAny<Wallet>())).ReturnsAsync((Wallet)null!);

        var act = () => CreateManager().GetWalletAsync(UserId);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Failed to create wallet for user 42*");
    }

    // -------------------------------------------------------
    //  CreditSellerAsync
    // -------------------------------------------------------

    [Fact]
    public async Task CreditSellerAsync_WhenWalletExists_CreditsAndRecordsTransaction()
    {
        var wallet = CreateWallet(SellerId, balance: 100m);
        const int orderId = 55;
        const decimal amount = 71.25m;

        _walletRepoMock.Setup(r => r.GetByUserIdWithLockAsync(SellerId)).ReturnsAsync(wallet);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await CreateManager().CreditSellerAsync(SellerId, amount, orderId);

        wallet.Balance.Should().Be(100m + amount * FinancialConstants.ProductSellerShare);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _walletRepoMock.Verify(r => r.AddTransactionAsync(It.Is<WalletTransaction>(
            t => t.Type == TransactionType.SellerCreditHeld
              && t.Amount == amount * FinancialConstants.ProductSellerShare
              && t.ReferenceType == "Order"
              && t.ReferenceId == orderId
        )), Times.Once);
    }

    [Fact]
    public async Task CreditSellerAsync_WhenWalletMissing_CreatesAndThenCredits()
    {
        var wallet = CreateWallet(SellerId, balance: 0m);
        const int orderId = 56;
        const decimal amount = 100m;

        _walletRepoMock.SetupSequence(r => r.GetByUserIdWithLockAsync(It.IsAny<int>()))
            .ReturnsAsync((Wallet?)null)
            .ReturnsAsync((Wallet?)null)
            .ReturnsAsync(wallet);
        _walletRepoMock.Setup(r => r.GetByUserIdAsync(SellerId)).ReturnsAsync((Wallet?)null);
        _walletRepoMock.Setup(r => r.CreateAsync(It.IsAny<Wallet>())).ReturnsAsync((Wallet)null!);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await CreateManager().CreditSellerAsync(SellerId, amount, orderId);

        wallet.Balance.Should().Be(amount * FinancialConstants.ProductSellerShare);
        _walletRepoMock.Verify(r => r.CreateAsync(It.Is<Wallet>(w => w.UserId == SellerId)), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _walletRepoMock.Verify(r => r.AddTransactionAsync(It.Is<WalletTransaction>(t => t.Type == TransactionType.SellerCreditHeld)), Times.Once);
    }

    [Fact]
    public async Task CreditSellerAsync_WhenCreationFails_ThrowsKeyNotFoundException()
    {
        const int orderId = 57;
        const decimal amount = 50m;

        _walletRepoMock.SetupSequence(r => r.GetByUserIdWithLockAsync(It.IsAny<int>()))
            .ReturnsAsync((Wallet?)null)
            .ReturnsAsync((Wallet?)null)
            .ReturnsAsync((Wallet?)null);

        _walletRepoMock.Setup(r => r.GetByUserIdAsync(SellerId)).ReturnsAsync((Wallet?)null);
        _walletRepoMock.Setup(r => r.CreateAsync(It.IsAny<Wallet>())).ReturnsAsync((Wallet)null!);

        var act = () => CreateManager().CreditSellerAsync(SellerId, amount, orderId);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Failed to create wallet for user 7*");
        _walletRepoMock.Verify(r => r.AddTransactionAsync(It.IsAny<WalletTransaction>()), Times.Never);
    }

    // -------------------------------------------------------
    //  CreditPlatformFeeAsync
    // -------------------------------------------------------

    [Fact]
    public async Task CreditPlatformFeeAsync_WhenWalletExists_CreditsAndSaves()
    {
        var wallet = CreateWallet(PlatformUserId, balance: 10m);
        const decimal amount = 3.75m;
        const string refType = "Order";
        const int refId = 55;

        _walletRepoMock.Setup(r => r.GetByUserIdWithLockAsync(PlatformUserId)).ReturnsAsync(wallet);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await CreateManager().CreditPlatformFeeAsync(PlatformUserId, amount, refType, refId);

        wallet.Balance.Should().Be(10m + amount);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        wallet.Transactions.Should().ContainSingle(t =>
            t.Type == TransactionType.PlatformFee
            && t.Amount == amount
            && t.ReferenceType == refType
            && t.ReferenceId == refId);
    }

    [Fact]
    public async Task CreditPlatformFeeAsync_WhenWalletMissing_CreatesAndThenCredits()
    {
        var wallet = CreateWallet(PlatformUserId, balance: 0m);
        const decimal amount = 5m;
        const string refType = "Auction";
        const int refId = 99;

        _walletRepoMock.SetupSequence(r => r.GetByUserIdWithLockAsync(It.IsAny<int>()))
            .ReturnsAsync((Wallet?)null)
            .ReturnsAsync((Wallet?)null)
            .ReturnsAsync(wallet);

        _walletRepoMock.Setup(r => r.GetByUserIdAsync(PlatformUserId)).ReturnsAsync((Wallet?)null);
        _walletRepoMock.Setup(r => r.CreateAsync(It.IsAny<Wallet>())).ReturnsAsync((Wallet)null!);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await CreateManager().CreditPlatformFeeAsync(PlatformUserId, amount, refType, refId);

        wallet.Balance.Should().Be(amount);
        _walletRepoMock.Verify(r => r.CreateAsync(It.Is<Wallet>(w => w.UserId == PlatformUserId)), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreditPlatformFeeAsync_WhenCreationFails_ThrowsKeyNotFoundException()
    {
        const decimal amount = 5m;

        _walletRepoMock.SetupSequence(r => r.GetByUserIdWithLockAsync(It.IsAny<int>()))
            .ReturnsAsync((Wallet?)null)
            .ReturnsAsync((Wallet?)null)
            .ReturnsAsync((Wallet?)null);
        _walletRepoMock.Setup(r => r.GetByUserIdAsync(PlatformUserId)).ReturnsAsync((Wallet?)null);
        _walletRepoMock.Setup(r => r.CreateAsync(It.IsAny<Wallet>())).ReturnsAsync((Wallet)null!);

        var act = () => CreateManager().CreditPlatformFeeAsync(PlatformUserId, amount, "Order", 1);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Failed to create wallet for user 1*");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreditPlatformFeeAsync_WhenAmountIsZeroOrNegative_ThrowsInvalidOperationException()
    {
        var actZero = () => CreateManager().CreditPlatformFeeAsync(PlatformUserId, 0m, "Order", 1);
        var actNeg = () => CreateManager().CreditPlatformFeeAsync(PlatformUserId, -1m, "Order", 1);

        await actZero.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Fee amount must be positive");
        await actNeg.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Fee amount must be positive");
        _walletRepoMock.Verify(r => r.GetByUserIdWithLockAsync(It.IsAny<int>()), Times.Never);
    }

    // -------------------------------------------------------
    //  DeductForSubscriptionAsync
    // -------------------------------------------------------

    [Fact]
    public async Task DeductForSubscriptionAsync_WhenNoAmbientTransaction_CommitsOwnedTransaction()
    {
        var wallet = CreateWallet(UserId, balance: 500m);
        const int subscriptionId = 33;
        const decimal amount = 120m;
        _walletRepoMock.Setup(r => r.GetByUserIdWithLockAsync(UserId)).ReturnsAsync(wallet);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await CreateManager().DeductForSubscriptionAsync(UserId, amount, subscriptionId);

        wallet.Balance.Should().Be(380m);
        wallet.Transactions.Should().ContainSingle(t =>
            t.Amount == -amount &&
            t.Type == TransactionType.SubscriptionPayment &&
            t.ReferenceType == "Subscription" &&
            t.ReferenceId == subscriptionId);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _txMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _txMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeductForSubscriptionAsync_WhenAmbientTransactionExists_ReusesItWithoutCommit()
    {
        var wallet = CreateWallet(UserId, balance: 500m);
        const int subscriptionId = 34;
        const decimal amount = 125m;
        _unitOfWorkMock.SetupGet(u => u.CurrentTransaction).Returns(_txMock.Object);
        _walletRepoMock.Setup(r => r.GetByUserIdWithLockAsync(UserId)).ReturnsAsync(wallet);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await CreateManager().DeductForSubscriptionAsync(UserId, amount, subscriptionId);

        wallet.Balance.Should().Be(375m);
        wallet.Transactions.Should().ContainSingle(t =>
            t.Amount == -amount &&
            t.Type == TransactionType.SubscriptionPayment &&
            t.ReferenceType == "Subscription" &&
            t.ReferenceId == subscriptionId);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _txMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _txMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // -------------------------------------------------------
    //  GetOrCreateWalletAsync (indirect through callers)
    // -------------------------------------------------------

    [Fact]
    public async Task GetOrCreateWalletAsync_CreatesWalletOnlyOnce()
    {
        var newWallet = CreateWallet(UserId, balance: 0m);

        _walletRepoMock.SetupSequence(r => r.GetByUserIdWithLockAsync(UserId))
            .ReturnsAsync((Wallet?)null)
            .ReturnsAsync(newWallet);
        _walletRepoMock.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync((Wallet?)null);
        _walletRepoMock.Setup(r => r.CreateAsync(It.IsAny<Wallet>())).ReturnsAsync((Wallet)null!);

        var result = await CreateManager().GetWalletAsync(UserId);

        result.Should().NotBeNull();
        result.Balance.Should().Be(0m);
        _walletRepoMock.Verify(r => r.CreateAsync(It.IsAny<Wallet>()), Times.Once);
    }

    // -------------------------------------------------------
    //  Edge Cases
    // -------------------------------------------------------

    [Fact]
    public async Task GetWalletAsync_WithZeroBalanceWallet_ReturnsCorrectResponse()
    {
        var wallet = CreateWallet(UserId, balance: 0m, held: 0m);
        _walletRepoMock.Setup(r => r.GetByUserIdWithLockAsync(UserId)).ReturnsAsync(wallet);

        var result = await CreateManager().GetWalletAsync(UserId);

        result.Balance.Should().Be(0m);
        result.HeldBalance.Should().Be(0m);
        result.AvailableBalance.Should().Be(0m);
    }

    [Fact]
    public async Task GetWalletAsync_SequentialCalls_EachReturnsCorrectWallet()
    {
        var wallet = CreateWallet(UserId, balance: 100m);
        _walletRepoMock.Setup(r => r.GetByUserIdWithLockAsync(UserId)).ReturnsAsync(wallet);

        var manager = CreateManager();

        var result1 = await manager.GetWalletAsync(UserId);
        var result2 = await manager.GetWalletAsync(UserId);

        result1.Balance.Should().Be(100m);
        result2.Balance.Should().Be(100m);
        _walletRepoMock.Verify(r => r.GetByUserIdWithLockAsync(UserId), Times.Exactly(2));
        _walletRepoMock.Verify(r => r.CreateAsync(It.IsAny<Wallet>()), Times.Never);
    }

    [Fact]
    public async Task CreditSellerAsync_NegativeAmount_ThrowsInvalidOperationException()
    {
        var act = () => CreateManager().CreditSellerAsync(SellerId, -50m, 1);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Seller credit amount must be positive");
    }

    [Fact]
    public async Task CreditSellerAsync_ZeroAmount_ThrowsInvalidOperationException()
    {
        var act = () => CreateManager().CreditSellerAsync(SellerId, 0m, 1);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Seller credit amount must be positive");
    }

}
