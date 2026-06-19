using Microsoft.Extensions.Logging;
using Sayiad.Data.Data;
using Sayiad.Domain.Constants;
using Sayiad.Domain.Dtos.WalletDtos;

namespace Sayiad.Domain.Managers;

public class WalletManager : IWalletManager
{
    private readonly IWalletRepository _walletRepo;
    private readonly ISystemWalletRepository _systemWalletRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WalletManager> _logger;

    public WalletManager(
        IWalletRepository walletRepo,
        ISystemWalletRepository systemWalletRepo,
        IUnitOfWork unitOfWork,
        ILogger<WalletManager> logger)
    {
        _walletRepo = walletRepo;
        _systemWalletRepo = systemWalletRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<WalletResponse> GetWalletAsync(int userId)
    {
        var wallet = await GetOrCreateWalletAsync(userId);
        return MapWallet(wallet);
    }

    public async Task<WalletResponse> DepositAsync(int userId, decimal amount)
    {
        if (amount <= 0) throw new InvalidOperationException("Deposit amount must be positive");

        await using var tx = await _unitOfWork.BeginTransactionAsync();

        var wallet = await _walletRepo.GetByUserIdWithLockAsync(userId)
            ?? throw new KeyNotFoundException("Wallet not found");

        wallet.Balance += amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        var txn = new WalletTransaction
        {
            WalletId = wallet.Id,
            Amount = amount,
            Type = TransactionType.Deposit,
            ReferenceType = "Deposit",
            Description = $"Deposited {amount:N2} EGP",
            BalanceSnapshot = wallet.Balance,
            CreatedAt = DateTime.UtcNow
        };
        await _walletRepo.AddTransactionAsync(txn);

        await _unitOfWork.SaveChangesAsync();
        await tx.CommitAsync();

        _logger.LogInformation("Wallet deposit: User {UserId}, Amount {Amount}", userId, amount);
        return MapWallet(wallet);
    }

    public async Task<WalletResponse> WithdrawAsync(int userId, decimal amount)
    {
        if (amount <= 0) throw new InvalidOperationException("Withdrawal amount must be positive");

        await using var tx = await _unitOfWork.BeginTransactionAsync();

        var wallet = await _walletRepo.GetByUserIdWithLockAsync(userId)
            ?? throw new KeyNotFoundException("Wallet not found");

        if (wallet.AvailableBalance < amount)
            throw new InvalidOperationException("Insufficient available balance for withdrawal");

        wallet.Balance -= amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        var txn = new WalletTransaction
        {
            WalletId = wallet.Id,
            Amount = -amount,
            Type = TransactionType.Withdrawal,
            ReferenceType = "Withdrawal",
            Description = $"Withdrew {amount:N2} EGP",
            BalanceSnapshot = wallet.Balance,
            CreatedAt = DateTime.UtcNow
        };
        await _walletRepo.AddTransactionAsync(txn);

        await _unitOfWork.SaveChangesAsync();
        await tx.CommitAsync();

        _logger.LogInformation("Wallet withdrawal: User {UserId}, Amount {Amount}", userId, amount);
        return MapWallet(wallet);
    }

    public async Task HoldFundsAsync(int userId, decimal amount, string referenceType, int referenceId)
    {
        await using var tx = await _unitOfWork.BeginTransactionAsync();

        var wallet = await _walletRepo.GetByUserIdWithLockAsync(userId)
            ?? throw new KeyNotFoundException("Wallet not found");

        if (wallet.AvailableBalance < amount)
            throw new InvalidOperationException("Insufficient funds");

        wallet.HeldBalance += amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        var txn = new WalletTransaction
        {
            WalletId = wallet.Id,
            Amount = -amount,
            Type = TransactionType.HoldDeduction,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Description = $"Funds held for {referenceType} #{referenceId}",
            BalanceSnapshot = wallet.Balance,
            CreatedAt = DateTime.UtcNow
        };
        await _walletRepo.AddTransactionAsync(txn);

        await _unitOfWork.SaveChangesAsync();
        await tx.CommitAsync();

        _logger.LogInformation("Funds held: User {UserId}, Amount {Amount}, Ref {RefType}#{RefId}",
            userId, amount, referenceType, referenceId);
    }

    public async Task ReleaseHeldFundsAsync(int userId, decimal amount, string referenceType, int referenceId)
    {
        var isOwner = _unitOfWork.CurrentTransaction == null;
        var tx = isOwner
            ? await _unitOfWork.BeginTransactionAsync()
            : _unitOfWork.CurrentTransaction;
        try
        {
            var wallet = await _walletRepo.GetByUserIdWithLockAsync(userId)
                ?? throw new KeyNotFoundException("Wallet not found");

            wallet.HeldBalance -= amount;
            if (wallet.HeldBalance < 0) wallet.HeldBalance = 0;
            wallet.UpdatedAt = DateTime.UtcNow;

            var txn = new WalletTransaction
            {
                WalletId = wallet.Id,
                Amount = amount,
                Type = TransactionType.HoldRelease,
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                Description = $"Funds released from {referenceType} #{referenceId}",
                BalanceSnapshot = wallet.Balance,
                CreatedAt = DateTime.UtcNow
            };
            await _walletRepo.AddTransactionAsync(txn);

            await _unitOfWork.SaveChangesAsync();
            if (isOwner) await tx.CommitAsync();
        }
        catch
        {
            if (isOwner) await tx.RollbackAsync();
            throw;
        }
    }

    // N-06: Removed TransferFundsAsync — replaced by platform withdrawal flow

    public async Task DeductForOrderAsync(int userId, decimal amount, int orderId)
    {
        var isOwner = _unitOfWork.CurrentTransaction == null;
        var tx = isOwner
            ? await _unitOfWork.BeginTransactionAsync()
            : _unitOfWork.CurrentTransaction;
        try
        {
            var wallet = await _walletRepo.GetByUserIdWithLockAsync(userId)
                ?? throw new KeyNotFoundException("Wallet not found");

            if (wallet.AvailableBalance < amount)
                throw new InvalidOperationException("Insufficient funds for order");

            wallet.Balance -= amount;
            wallet.HeldBalance = Math.Max(0, wallet.HeldBalance - amount);
            wallet.UpdatedAt = DateTime.UtcNow;

            var txn = new WalletTransaction
            {
                WalletId = wallet.Id,
                Amount = -amount,
                Type = TransactionType.OrderPayment,
                ReferenceType = "Order",
                ReferenceId = orderId,
                Description = $"Payment for order #{orderId}",
                BalanceSnapshot = wallet.Balance,
                CreatedAt = DateTime.UtcNow
            };
            await _walletRepo.AddTransactionAsync(txn);

            await _unitOfWork.SaveChangesAsync();
            if (isOwner) await tx.CommitAsync();
        }
        catch
        {
            if (isOwner) await tx.RollbackAsync();
            throw;
        }
    }

    public async Task CreditSellerAsync(int sellerId, decimal amount, int orderId)
    {
        if (amount <= 0) throw new InvalidOperationException("Seller credit amount must be positive");

        var sellerShare = amount * FinancialConstants.ProductSellerShare;

        var isOwner = _unitOfWork.CurrentTransaction == null;
        var tx = isOwner
            ? await _unitOfWork.BeginTransactionAsync()
            : _unitOfWork.CurrentTransaction;
        try
        {
            var wallet = await _walletRepo.GetByUserIdWithLockAsync(sellerId)
                ?? await GetOrCreateWalletAsync(sellerId);

            wallet.Balance += sellerShare;
            wallet.HeldBalance += sellerShare;
            wallet.FreezeUntil = DateTime.UtcNow.AddDays(FinancialConstants.ProductFreezeDays);
            wallet.UpdatedAt = DateTime.UtcNow;

            var txn = new WalletTransaction
            {
                WalletId = wallet.Id,
                Amount = sellerShare,
                Type = TransactionType.SellerCreditHeld,
                ReferenceType = "Order",
                ReferenceId = orderId,
                Description = $"Payout for order #{orderId} ({FinancialConstants.ProductSellerShare:P0}) — frozen {FinancialConstants.ProductFreezeDays}d",
                BalanceSnapshot = wallet.Balance,
                CreatedAt = DateTime.UtcNow
            };
            await _walletRepo.AddTransactionAsync(txn);

            await _unitOfWork.SaveChangesAsync();
            if (isOwner) await tx.CommitAsync();

            _logger.LogInformation("Seller credited: User {SellerId}, Gross {Amount}, Net {Net}, Order {OrderId}",
                sellerId, amount, sellerShare, orderId);
        }
        catch
        {
            if (isOwner) await tx.RollbackAsync();
            throw;
        }
    }

    public async Task SettleAuctionPaymentAsync(int winnerId, int sellerId, decimal winningAmount, int auctionId, int auctioneerId)
    {
        if (winningAmount <= 0)
            throw new InvalidOperationException("Winning amount must be positive");

        var isOwner = _unitOfWork.CurrentTransaction == null;
        var tx = isOwner
            ? await _unitOfWork.BeginTransactionAsync()
            : _unitOfWork.CurrentTransaction!;

        try
        {
            var winnerWallet = await _walletRepo.GetByUserIdWithLockAsync(winnerId)
                ?? throw new KeyNotFoundException("Winner wallet not found");
            var sellerWallet = await _walletRepo.GetByUserIdWithLockAsync(sellerId)
                ?? throw new KeyNotFoundException("Seller wallet not found");

            // N-02: 3-way split from FinancialConstants
            var sellerAmount = winningAmount * FinancialConstants.AuctionFishermanShare;
            var auctioneerAmount = winningAmount * FinancialConstants.AuctionAuctioneerFee;
            var platformAmount = winningAmount * FinancialConstants.AuctionPlatformFee;

            // Deduct from winner
            if (winnerWallet.AvailableBalance < winningAmount)
                throw new InvalidOperationException("Winner has insufficient available balance to settle auction payment");

            winnerWallet.Balance -= winningAmount;
            winnerWallet.HeldBalance = Math.Max(0, winnerWallet.HeldBalance - winningAmount);
            winnerWallet.UpdatedAt = DateTime.UtcNow;

            winnerWallet.Transactions.Add(new WalletTransaction
            {
                Amount = -winningAmount,
                Type = TransactionType.AuctioneerFee,
                ReferenceType = "Auction",
                ReferenceId = auctionId,
                Description = $"Payment for winning auction #{auctionId}",
                BalanceSnapshot = winnerWallet.Balance,
                CreatedAt = DateTime.UtcNow
            });

            // Credit seller (no freeze — auction delivery is in-person)
            sellerWallet.Balance += sellerAmount;
            sellerWallet.UpdatedAt = DateTime.UtcNow;

            sellerWallet.Transactions.Add(new WalletTransaction
            {
                Amount = sellerAmount,
                Type = TransactionType.SellerCredit,
                ReferenceType = "Auction",
                ReferenceId = auctionId,
                Description = $"Payout for auction #{auctionId} ({FinancialConstants.AuctionFishermanShare:P0})",
                BalanceSnapshot = sellerWallet.Balance,
                CreatedAt = DateTime.UtcNow
            });

            // Credit auctioneer
            var auctioneerWallet = await _walletRepo.GetByUserIdWithLockAsync(auctioneerId);
            if (auctioneerWallet != null)
            {
                auctioneerWallet.Balance += auctioneerAmount;
                auctioneerWallet.UpdatedAt = DateTime.UtcNow;

                auctioneerWallet.Transactions.Add(new WalletTransaction
                {
                    Amount = auctioneerAmount,
                    Type = TransactionType.AuctioneerFee,
                    ReferenceType = "Auction",
                    ReferenceId = auctionId,
                    Description = $"Auctioneer fee for auction #{auctionId} ({FinancialConstants.AuctionAuctioneerFee:P0})",
                    BalanceSnapshot = auctioneerWallet.Balance,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Credit platform via SystemWallet
            var systemWallet = await _systemWalletRepo.GetWithLockAsync();
            if (systemWallet != null)
            {
                systemWallet.Balance += platformAmount;
                systemWallet.UpdatedAt = DateTime.UtcNow;

                await _systemWalletRepo.AddTransactionAsync(new SystemWalletTransaction
                {
                    SystemWalletId = systemWallet.Id,
                    Amount = platformAmount,
                    Type = SystemTransactionType.AuctioneerFeeCredit,
                    ReferenceType = "Auction",
                    ReferenceId = auctionId,
                    Description = $"Platform fee for auction #{auctionId} ({FinancialConstants.AuctionPlatformFee:P0})",
                    BalanceSnapshot = systemWallet.Balance,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _unitOfWork.SaveChangesAsync();
            if (isOwner) await tx.CommitAsync();

            _logger.LogInformation(
                "Auction payment settled: Winner {WinnerId}, Seller {SellerId}, " +
                "Amount {Amount}, SellerShare {SellerShare}, AuctioneerShare {AuctioneerShare}, PlatformShare {PlatformShare}, Auction {AuctionId}",
                winnerId, sellerId, winningAmount, sellerAmount, auctioneerAmount, platformAmount, auctionId);
        }
        catch
        {
            if (isOwner) await tx.RollbackAsync();
            throw;
        }
        finally
        {
            if (isOwner) await tx.DisposeAsync();
        }
    }

    public async Task CreditPlatformFeeAsync(int platformUserId, decimal amount, string referenceType, int referenceId)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Fee amount must be positive");

        var isOwner = _unitOfWork.CurrentTransaction == null;
        var tx = isOwner
            ? await _unitOfWork.BeginTransactionAsync()
            : _unitOfWork.CurrentTransaction;
        try
        {
            // Record on admin user wallet
            var wallet = await _walletRepo.GetByUserIdWithLockAsync(platformUserId)
                ?? await GetOrCreateWalletAsync(platformUserId);

            wallet.Balance += amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            wallet.Transactions.Add(new WalletTransaction
            {
                Amount = amount,
                Type = TransactionType.PlatformFee,
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                Description = $"Platform fee for {referenceType} #{referenceId} ({FinancialConstants.ProductPlatformFee:P0})",
                BalanceSnapshot = wallet.Balance,
                CreatedAt = DateTime.UtcNow
            });

            // Also credit SystemWallet
            var systemWallet = await _systemWalletRepo.GetWithLockAsync();
            if (systemWallet != null)
            {
                systemWallet.Balance += amount;
                systemWallet.UpdatedAt = DateTime.UtcNow;

                await _systemWalletRepo.AddTransactionAsync(new SystemWalletTransaction
                {
                    SystemWalletId = systemWallet.Id,
                    Amount = amount,
                    Type = SystemTransactionType.PlatformFeeCredit,
                    ReferenceType = referenceType,
                    ReferenceId = referenceId,
                    Description = $"Platform fee for {referenceType} #{referenceId}",
                    BalanceSnapshot = systemWallet.Balance,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _unitOfWork.SaveChangesAsync();
            if (isOwner) await tx.CommitAsync();
        }
        catch
        {
            if (isOwner) await tx.RollbackAsync();
            throw;
        }
    }

    public async Task DeductForSubscriptionAsync(int userId, decimal amount, int subscriptionId)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Subscription amount must be positive");

        var isOwner = _unitOfWork.CurrentTransaction == null;
        var tx = isOwner
            ? await _unitOfWork.BeginTransactionAsync()
            : _unitOfWork.CurrentTransaction!;

        try
        {
            var wallet = await _walletRepo.GetByUserIdWithLockAsync(userId)
                ?? throw new KeyNotFoundException("Wallet not found");

            if (wallet.AvailableBalance < amount)
                throw new InvalidOperationException("Insufficient balance for subscription upgrade");

            wallet.Balance -= amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            wallet.Transactions.Add(new WalletTransaction
            {
                Amount = -amount,
                Type = TransactionType.SubscriptionPayment,
                ReferenceType = "Subscription",
                ReferenceId = subscriptionId,
                Description = $"Payment for subscription #{subscriptionId}",
                BalanceSnapshot = wallet.Balance,
                CreatedAt = DateTime.UtcNow
            });

            await _unitOfWork.SaveChangesAsync();
            if (isOwner) await tx.CommitAsync();
        }
        catch
        {
            if (isOwner) await tx.RollbackAsync();
            throw;
        }
        finally
        {
            if (isOwner) await tx.DisposeAsync();
        }

        _logger.LogInformation(
            "Subscription payment: User {UserId}, Amount {Amount}, Subscription {SubscriptionId}",
            userId, amount, subscriptionId);
    }

    // N-01: Freeze payout into HeldBalance
    public async Task ApplyPayoutFreezeAsync(int userId, decimal amount, int freezeDays)
    {
        if (amount <= 0) throw new InvalidOperationException("Freeze amount must be positive");
        if (freezeDays <= 0) throw new InvalidOperationException("Freeze days must be positive");

        await using var tx = await _unitOfWork.BeginTransactionAsync();

        var wallet = await _walletRepo.GetByUserIdWithLockAsync(userId)
            ?? throw new KeyNotFoundException("Wallet not found");

        if (wallet.Balance < amount)
            throw new InvalidOperationException("Insufficient balance to freeze");

        wallet.HeldBalance += amount;
        wallet.FreezeUntil = DateTime.UtcNow.AddDays(freezeDays);
        wallet.UpdatedAt = DateTime.UtcNow;

        var txn = new WalletTransaction
        {
            WalletId = wallet.Id,
            Amount = -amount,
            Type = TransactionType.HoldDeduction,
            ReferenceType = "Freeze",
            ReferenceId = null,
            Description = $"Payout frozen for {freezeDays} days",
            BalanceSnapshot = wallet.Balance,
            CreatedAt = DateTime.UtcNow
        };
        await _walletRepo.AddTransactionAsync(txn);

        await _unitOfWork.SaveChangesAsync();
        await tx.CommitAsync();

        _logger.LogInformation("Payout frozen: User {UserId}, Amount {Amount}, Days {Days}", userId, amount, freezeDays);
    }

    // N-05: Release expired freeze
    public async Task ReleaseExpiredFreezeAsync(int walletId)
    {
        var wallet = await _walletRepo.GetByUserIdWithLockAsync(walletId);
        if (wallet == null || wallet.HeldBalance <= 0 || wallet.FreezeUntil == null) return;

        var amount = wallet.HeldBalance;
        wallet.HeldBalance = 0;
        wallet.FreezeUntil = null;
        wallet.UpdatedAt = DateTime.UtcNow;

        var txn = new WalletTransaction
        {
            WalletId = wallet.Id,
            Amount = amount,
            Type = TransactionType.HoldRelease,
            ReferenceType = "Freeze",
            ReferenceId = null,
            Description = $"Frozen payout released after freeze period",
            BalanceSnapshot = wallet.Balance,
            CreatedAt = DateTime.UtcNow
        };
        await _walletRepo.AddTransactionAsync(txn);

        _logger.LogInformation("Freeze released: Wallet {WalletId}, Amount {Amount}", wallet.Id, amount);
    }

    // Return flow: reverse seller payout
    public async Task ReverseSellerPayoutAsync(int sellerId, decimal amount, int orderId)
    {
        var isOwner = _unitOfWork.CurrentTransaction == null;
        var tx = isOwner
            ? await _unitOfWork.BeginTransactionAsync()
            : _unitOfWork.CurrentTransaction;
        try
        {
            var wallet = await _walletRepo.GetByUserIdWithLockAsync(sellerId)
                ?? throw new KeyNotFoundException("Seller wallet not found");

            var sellerPayout = amount * FinancialConstants.ProductSellerShare;

            // Reduce from held balance first, then balance (no negative)
            var heldReduction = Math.Min(sellerPayout, wallet.HeldBalance);
            wallet.HeldBalance -= heldReduction;
            var balanceReduction = Math.Min(sellerPayout - heldReduction, wallet.Balance);
            wallet.Balance -= balanceReduction;
            if (wallet.HeldBalance < 0) wallet.HeldBalance = 0;
            if (balanceReduction < sellerPayout - heldReduction)
                _logger.LogWarning("Seller {SellerId} payout reversal shortfall: {Shortfall}",
                    sellerId, sellerPayout - heldReduction - balanceReduction);
            wallet.UpdatedAt = DateTime.UtcNow;

            wallet.Transactions.Add(new WalletTransaction
            {
                Amount = -sellerPayout,
                Type = TransactionType.OrderRefund,
                ReferenceType = "Order",
                ReferenceId = orderId,
                Description = $"Return reversal for order #{orderId}",
                BalanceSnapshot = wallet.Balance,
                CreatedAt = DateTime.UtcNow
            });

            await _unitOfWork.SaveChangesAsync();
            if (isOwner) await tx.CommitAsync();
        }
        catch
        {
            if (isOwner) await tx.RollbackAsync();
            throw;
        }
    }

    // Return flow: refund buyer
    public async Task RefundBuyerAsync(int buyerId, decimal amount, int orderId)
    {
        var isOwner = _unitOfWork.CurrentTransaction == null;
        var tx = isOwner
            ? await _unitOfWork.BeginTransactionAsync()
            : _unitOfWork.CurrentTransaction;
        try
        {
            var wallet = await _walletRepo.GetByUserIdWithLockAsync(buyerId)
                ?? await GetOrCreateWalletAsync(buyerId);

            wallet.Balance += amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            wallet.Transactions.Add(new WalletTransaction
            {
                Amount = amount,
                Type = TransactionType.OrderRefund,
                ReferenceType = "Order",
                ReferenceId = orderId,
                Description = $"Refund for returned order #{orderId}",
                BalanceSnapshot = wallet.Balance,
                CreatedAt = DateTime.UtcNow
            });

            await _unitOfWork.SaveChangesAsync();
            if (isOwner) await tx.CommitAsync();
        }
        catch
        {
            if (isOwner) await tx.RollbackAsync();
            throw;
        }
    }

    // Return flow: reverse platform fee from SystemWallet
    public async Task ReversePlatformFeeAsync(decimal amount, int orderId)
    {
        var isOwner = _unitOfWork.CurrentTransaction == null;
        var tx = isOwner
            ? await _unitOfWork.BeginTransactionAsync()
            : _unitOfWork.CurrentTransaction;
        try
        {
            var systemWallet = await _systemWalletRepo.GetWithLockAsync();
            if (systemWallet == null) return;

            var feeAmount = amount * FinancialConstants.ProductPlatformFee;
            var heldReduction = Math.Min(feeAmount, systemWallet.HeldBalance);
            systemWallet.HeldBalance -= heldReduction;
            var balanceReduction = Math.Min(feeAmount - heldReduction, systemWallet.Balance);
            systemWallet.Balance -= balanceReduction;
            systemWallet.HeldBalance = Math.Max(0, systemWallet.HeldBalance);
            systemWallet.Balance = Math.Max(0, systemWallet.Balance);

            if (balanceReduction < feeAmount - heldReduction)
                _logger.LogWarning("SystemWallet shortfall on fee reversal for order {OrderId}: {Shortfall}",
                    orderId, feeAmount - heldReduction - balanceReduction);
            systemWallet.UpdatedAt = DateTime.UtcNow;

            await _systemWalletRepo.AddTransactionAsync(new SystemWalletTransaction
            {
                SystemWalletId = systemWallet.Id,
                Amount = -feeAmount,
                Type = SystemTransactionType.PlatformFeeRefunded,
                ReferenceType = "Order",
                ReferenceId = orderId,
                Description = $"Platform fee refund for returned order #{orderId}",
                BalanceSnapshot = systemWallet.Balance,
                CreatedAt = DateTime.UtcNow
            });

            await _unitOfWork.SaveChangesAsync();
            if (isOwner) await tx.CommitAsync();

            _logger.LogInformation("Platform fee reversed: Order {OrderId}, Amount {Amount}", orderId, feeAmount);
        }
        catch
        {
            if (isOwner) await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<WalletTransactionsResponse> GetTransactionsAsync(int userId, PaginationRequest pagination)
    {
        var wallet = await _walletRepo.GetByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("Wallet not found");

        var items = await _walletRepo.GetTransactionsAsync(wallet.Id, pagination);
        var totalCount = await _walletRepo.GetTransactionCountAsync(wallet.Id);

        return new WalletTransactionsResponse(
            items.Select(t => new WalletTransactionResponse(
                t.Id, t.Amount, t.Type.ToString(), t.ReferenceType, t.ReferenceId,
                t.Description, t.BalanceSnapshot, t.CreatedAt)).ToList(),
            totalCount,
            pagination.Page,
            pagination.PageSize
        );
    }

    public async Task<bool> HasSufficientBalanceAsync(int userId, decimal amount)
    {
        var wallet = await _walletRepo.GetByUserIdWithLockAsync(userId);
        if (wallet == null) return false;
        return wallet.AvailableBalance >= amount;
    }

    private async Task<Wallet> GetOrCreateWalletAsync(int userId)
    {
        await using var tx = await _unitOfWork.BeginTransactionAsync();
        var wallet = await _walletRepo.GetByUserIdWithLockAsync(userId);
        if (wallet == null)
        {
            await CreateWalletAsync(userId);
            wallet = await _walletRepo.GetByUserIdWithLockAsync(userId)
                ?? throw new KeyNotFoundException($"Failed to create wallet for user {userId}");
        }
        await tx.CommitAsync();
        return wallet;
    }

    public async Task<bool> WalletExistsAsync(int userId)
    {
        var wallet = await _walletRepo.GetByUserIdAsync(userId);
        return wallet != null;
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
    }

    private static WalletResponse MapWallet(Wallet w) => new(w.Balance, w.HeldBalance, w.AvailableBalance, w.CreatedAt, w.FreezeUntil);
}
