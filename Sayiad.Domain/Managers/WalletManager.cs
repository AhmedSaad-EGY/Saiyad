using Microsoft.Extensions.Logging;
using Sayiad.Data.Data;
using Sayiad.Domain.Dtos.WalletDtos;

namespace Sayiad.Domain.Managers;

public class WalletManager : IWalletManager
{
    private readonly IWalletRepository _walletRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WalletManager> _logger;

    public WalletManager(IWalletRepository walletRepo, IUnitOfWork unitOfWork, ILogger<WalletManager> logger)
    {
        _walletRepo = walletRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<WalletResponse> GetWalletAsync(int userId)
    {
        var wallet = await _walletRepo.GetByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("Wallet not found");
        return MapWallet(wallet);
    }

    public async Task<WalletResponse> DepositAsync(int userId, decimal amount)
    {
        if (amount <= 0) throw new InvalidOperationException("Deposit amount must be positive");

        var wallet = await _walletRepo.GetByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("Wallet not found");

        wallet.Balance += amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        await _walletRepo.UpdateAsync(wallet);

        var txn = new WalletTransaction
        {
            WalletId = wallet.Id,
            Amount = amount,
            Type = "Deposit",
            ReferenceType = "Deposit",
            Description = $"Deposited {amount:N2} EGP",
            BalanceSnapshot = wallet.Balance,
            CreatedAt = DateTime.UtcNow
        };
        await _walletRepo.AddTransactionAsync(txn);

        _logger.LogInformation("Wallet deposit: User {UserId}, Amount {Amount}", userId, amount);
        return MapWallet(wallet);
    }

    public async Task HoldFundsAsync(int userId, decimal amount, string referenceType, int referenceId)
    {
        var wallet = await _walletRepo.GetByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("Wallet not found");

        if (wallet.AvailableBalance < amount)
            throw new InvalidOperationException("Insufficient funds");

        wallet.HeldBalance += amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        await _walletRepo.UpdateAsync(wallet);

        var txn = new WalletTransaction
        {
            WalletId = wallet.Id,
            Amount = -amount,
            Type = "Hold",
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Description = $"Funds held for {referenceType} #{referenceId}",
            BalanceSnapshot = wallet.Balance,
            CreatedAt = DateTime.UtcNow
        };
        await _walletRepo.AddTransactionAsync(txn);

        _logger.LogInformation("Funds held: User {UserId}, Amount {Amount}, Ref {RefType}#{RefId}",
            userId, amount, referenceType, referenceId);
    }

    public async Task ReleaseHeldFundsAsync(int userId, decimal amount, string referenceType, int referenceId)
    {
        var wallet = await _walletRepo.GetByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("Wallet not found");

        wallet.HeldBalance -= amount;
        if (wallet.HeldBalance < 0) wallet.HeldBalance = 0;
        wallet.UpdatedAt = DateTime.UtcNow;

        await _walletRepo.UpdateAsync(wallet);

        var txn = new WalletTransaction
        {
            WalletId = wallet.Id,
            Amount = amount,
            Type = "Release",
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Description = $"Funds released from {referenceType} #{referenceId}",
            BalanceSnapshot = wallet.Balance,
            CreatedAt = DateTime.UtcNow
        };
        await _walletRepo.AddTransactionAsync(txn);

        _logger.LogInformation("Funds released: User {UserId}, Amount {Amount}, Ref {RefType}#{RefId}",
            userId, amount, referenceType, referenceId);
    }

    public async Task TransferFundsAsync(int fromUserId, int toUserId, decimal amount, string description)
    {
        if (amount <= 0) throw new InvalidOperationException("Transfer amount must be positive");

        var fromWallet = await _walletRepo.GetByUserIdAsync(fromUserId)
            ?? throw new KeyNotFoundException("Sender wallet not found");
        var toWallet = await _walletRepo.GetByUserIdAsync(toUserId)
            ?? throw new KeyNotFoundException("Receiver wallet not found");

        fromWallet.Balance -= amount;
        fromWallet.HeldBalance -= amount;
        if (fromWallet.HeldBalance < 0) fromWallet.HeldBalance = 0;
        fromWallet.UpdatedAt = DateTime.UtcNow;

        toWallet.Balance += amount;
        toWallet.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        var fromTxn = new WalletTransaction
        {
            WalletId = fromWallet.Id,
            Amount = -amount,
            Type = "Transfer",
            ReferenceType = "Transfer",
            Description = description,
            BalanceSnapshot = fromWallet.Balance,
            CreatedAt = DateTime.UtcNow
        };
        var toTxn = new WalletTransaction
        {
            WalletId = toWallet.Id,
            Amount = amount,
            Type = "Transfer",
            ReferenceType = "Transfer",
            Description = description,
            BalanceSnapshot = toWallet.Balance,
            CreatedAt = DateTime.UtcNow
        };

        await _walletRepo.AddTransactionAsync(fromTxn);
        await _walletRepo.AddTransactionAsync(toTxn);

        _logger.LogInformation("Transfer: From User {FromId} → To User {ToId}, Amount {Amount}",
            fromUserId, toUserId, amount);
    }

    public async Task DeductForOrderAsync(int userId, decimal amount, int orderId)
    {
        var wallet = await _walletRepo.GetByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("Wallet not found");

        if (wallet.AvailableBalance < amount)
            throw new InvalidOperationException("Insufficient funds for order");

        wallet.Balance -= amount;
        wallet.HeldBalance = Math.Max(0, wallet.HeldBalance - amount);
        wallet.UpdatedAt = DateTime.UtcNow;

        await _walletRepo.UpdateAsync(wallet);

        var txn = new WalletTransaction
        {
            WalletId = wallet.Id,
            Amount = -amount,
            Type = "Debit",
            ReferenceType = "Order",
            ReferenceId = orderId,
            Description = $"Payment for order #{orderId}",
            BalanceSnapshot = wallet.Balance,
            CreatedAt = DateTime.UtcNow
        };
        await _walletRepo.AddTransactionAsync(txn);

        _logger.LogInformation("Order payment: User {UserId}, Order {OrderId}, Amount {Amount}",
            userId, orderId, amount);
    }

    public async Task CreditSellerAsync(int sellerId, decimal amount, int orderId)
    {
        var wallet = await _walletRepo.GetByUserIdAsync(sellerId)
            ?? throw new KeyNotFoundException("Seller wallet not found");

        wallet.Balance += amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        await _walletRepo.UpdateAsync(wallet);

        var txn = new WalletTransaction
        {
            WalletId = wallet.Id,
            Amount = amount,
            Type = "Credit",
            ReferenceType = "Order",
            ReferenceId = orderId,
            Description = $"Payout for order #{orderId} (95%)",
            BalanceSnapshot = wallet.Balance,
            CreatedAt = DateTime.UtcNow
        };
        await _walletRepo.AddTransactionAsync(txn);

        _logger.LogInformation("Seller credit: User {SellerId}, Order {OrderId}, Amount {Amount}",
            sellerId, orderId, amount);
    }

    public async Task SettleAuctionPaymentAsync(int winnerId, int sellerId, decimal winningAmount, int auctionId)
    {
        if (winningAmount <= 0)
            throw new InvalidOperationException("Winning amount must be positive");

        var winnerWallet = await _walletRepo.GetByUserIdAsync(winnerId)
            ?? throw new KeyNotFoundException("Winner wallet not found");
        var sellerWallet = await _walletRepo.GetByUserIdAsync(sellerId)
            ?? throw new KeyNotFoundException("Seller wallet not found");

        var platformFee = winningAmount * 0.05m;
        var sellerAmount = winningAmount - platformFee;

        winnerWallet.Balance -= winningAmount;
        winnerWallet.HeldBalance = Math.Max(0, winnerWallet.HeldBalance - winningAmount);
        winnerWallet.UpdatedAt = DateTime.UtcNow;

        sellerWallet.Balance += sellerAmount;
        sellerWallet.UpdatedAt = DateTime.UtcNow;

        winnerWallet.Transactions.Add(new WalletTransaction
        {
            Amount = -winningAmount,
            Type = "AuctionPayment",
            ReferenceType = "Auction",
            ReferenceId = auctionId,
            Description = $"Payment for winning auction #{auctionId}",
            BalanceSnapshot = winnerWallet.Balance,
            CreatedAt = DateTime.UtcNow
        });

        sellerWallet.Transactions.Add(new WalletTransaction
        {
            Amount = sellerAmount,
            Type = "AuctionPayout",
            ReferenceType = "Auction",
            ReferenceId = auctionId,
            Description = $"Payout for auction #{auctionId} (95%)",
            BalanceSnapshot = sellerWallet.Balance,
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Auction payment settled: Winner {WinnerId}, Seller {SellerId}, " +
            "WinningAmount {Amount}, Fee {Fee}, Auction {AuctionId}",
            winnerId, sellerId, winningAmount, platformFee, auctionId);
    }

    public async Task CreditPlatformFeeAsync(int platformUserId, decimal amount, string referenceType, int referenceId)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Fee amount must be positive");

        var wallet = await _walletRepo.GetByUserIdAsync(platformUserId)
            ?? throw new KeyNotFoundException("Platform wallet not found");

        wallet.Balance += amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        wallet.Transactions.Add(new WalletTransaction
        {
            Amount = amount,
            Type = "PlatformFee",
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Description = $"Platform fee for {referenceType} #{referenceId} (5%)",
            BalanceSnapshot = wallet.Balance,
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Platform fee credited: User {PlatformUserId}, Amount {Amount}, Ref {RefType}#{RefId}",
            platformUserId, amount, referenceType, referenceId);
    }

    public async Task DeductForSubscriptionAsync(int userId, decimal amount, int subscriptionId)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Subscription amount must be positive");

        var wallet = await _walletRepo.GetByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("Wallet not found");

        if (wallet.AvailableBalance < amount)
            throw new InvalidOperationException("Insufficient balance for subscription upgrade");

        wallet.Balance -= amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        wallet.Transactions.Add(new WalletTransaction
        {
            Amount = -amount,
            Type = "SubscriptionPayment",
            ReferenceType = "Subscription",
            ReferenceId = subscriptionId,
            Description = $"Payment for subscription #{subscriptionId}",
            BalanceSnapshot = wallet.Balance,
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Subscription payment: User {UserId}, Amount {Amount}, Subscription {SubscriptionId}",
            userId, amount, subscriptionId);
    }

    public async Task<WalletTransactionsResponse> GetTransactionsAsync(int userId, PaginationRequest pagination)
    {
        var wallet = await _walletRepo.GetByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("Wallet not found");

        var items = await _walletRepo.GetTransactionsAsync(wallet.Id, pagination);
        var totalCount = await _walletRepo.GetTransactionCountAsync(wallet.Id);

        return new WalletTransactionsResponse
        {
            Items = items.Select(t => new WalletTransactionResponse(
                t.Id, t.Amount, t.Type, t.ReferenceType, t.ReferenceId,
                t.Description, t.BalanceSnapshot, t.CreatedAt)).ToList(),
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    public async Task<bool> HasSufficientBalanceAsync(int userId, decimal amount)
    {
        var wallet = await _walletRepo.GetByUserIdAsync(userId);
        if (wallet == null) return false;
        return wallet.AvailableBalance >= amount;
    }

    public async Task CreateWalletAsync(int userId)
    {
        var existing = await _walletRepo.GetByUserIdAsync(userId);
        if (existing != null) return;

        var wallet = new Wallet
        {
            UserId = userId,
            Balance = 0,
            HeldBalance = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _walletRepo.CreateAsync(wallet);

        _logger.LogInformation("Wallet created for user {UserId}", userId);
    }

    private static WalletResponse MapWallet(Wallet w) => new(w.Balance, w.HeldBalance, w.AvailableBalance, w.CreatedAt);
}
