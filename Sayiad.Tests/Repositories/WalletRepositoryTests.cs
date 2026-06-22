using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Sayiad.Data.Common;
using Sayiad.Data.Data;
using Sayiad.Data.Models;
using Sayiad.Data.Repository.WalletRepo;

namespace Sayiad.Tests.Repositories;

public class WalletRepositoryTests
{
    [Fact]
    public async Task GetByUserIdAsync_DoesNotEagerLoadTransactionLedger()
    {
        await using var context = CreateContext();
        var wallet = CreateWallet(1, 42);
        context.Wallets.Add(wallet);
        context.WalletTransactions.Add(CreateTransaction(1, wallet.Id, DateTime.UtcNow));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result = await new WalletRepository(context).GetByUserIdAsync(wallet.UserId);

        result.Should().NotBeNull();
        result!.Transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTransactionsAsync_ReturnsOnlyRequestedWalletPageInNewestFirstOrder()
    {
        await using var context = CreateContext();
        var firstWallet = CreateWallet(1, 42);
        var secondWallet = CreateWallet(2, 43);
        context.Wallets.AddRange(firstWallet, secondWallet);
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        context.WalletTransactions.AddRange(
            Enumerable.Range(1, 25).Select(i => CreateTransaction(i, firstWallet.Id, start.AddMinutes(i))));
        context.WalletTransactions.Add(CreateTransaction(100, secondWallet.Id, start.AddDays(1)));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new WalletRepository(context);
        var result = await repository.GetTransactionsAsync(
            firstWallet.Id,
            new PaginationRequest { Page = 2, PageSize = 20 });

        result.Should().HaveCount(5);
        result.Select(transaction => transaction.Id).Should().Equal(5, 4, 3, 2, 1);
        result.Should().OnlyContain(transaction => transaction.WalletId == firstWallet.Id);
        (await repository.GetTransactionCountAsync(firstWallet.Id)).Should().Be(25);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Wallet CreateWallet(int id, int userId) => new()
    {
        Id = id,
        UserId = userId,
        Balance = 100m,
        HeldBalance = 0m,
        RowVersion = [],
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static WalletTransaction CreateTransaction(int id, int walletId, DateTime createdAt) => new()
    {
        Id = id,
        WalletId = walletId,
        Amount = id,
        Type = TransactionType.Deposit,
        ReferenceType = "Test",
        BalanceSnapshot = id,
        CreatedAt = createdAt
    };
}
