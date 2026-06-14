using Sayiad.Data.Data;
using Sayiad.Domain.Constants;
using Sayiad.Domain.Contracts;
using Sayiad.Domain.Managers;

namespace Sayiad.Api.Services.Background;

public class AuctionExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuctionExpiryService> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);

    public AuctionExpiryService(
        IServiceScopeFactory scopeFactory,
        ILogger<AuctionExpiryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Auction expiry service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckExpiredAuctionsAsync();
            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task CheckExpiredAuctionsAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var auctionRepo = scope.ServiceProvider
                .GetRequiredService<IAuctionRepository>();
            var unitOfWork = scope.ServiceProvider
                .GetRequiredService<IUnitOfWork>();
            var notificationManager = scope.ServiceProvider
                .GetRequiredService<INotificationManager>();
            var emailService = scope.ServiceProvider
                .GetRequiredService<IEmailService>();
            var walletManager = scope.ServiceProvider
                .GetRequiredService<IWalletManager>();
            var userRepo = scope.ServiceProvider
                .GetRequiredService<IUserRepository>();
            var db = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            // Activate scheduled auctions whose start time has arrived
            var toActivate = await db.Auctions
                .Where(a => a.Status == AuctionStatus.Scheduled && a.StartTime <= DateTime.UtcNow)
                .ToListAsync();

            foreach (var auction in toActivate)
            {
                auction.Status = AuctionStatus.Active;
                _logger.LogInformation("Auction {AuctionId} activated (scheduled start time reached)", auction.Id);
            }

            if (toActivate.Any())
                await unitOfWork.SaveChangesAsync();

            var expiredAuctions = await auctionRepo.GetExpiredActiveAsync();

            foreach (var auction in expiredAuctions)
            {
                try
                {
                    await using var tx = await unitOfWork.BeginTransactionAsync();

                    auction.Status = AuctionStatus.Finished;

                    var winningBid = auction.Bids
                        .Where(b => b.BidStatus == BidStatus.Winning)
                        .MaxBy(b => b.Amount);

                    if (winningBid != null && auction.ReservePrice > 0 && winningBid.Amount < auction.ReservePrice)
                    {
                        auction.Status = AuctionStatus.PendingSellerConfirmation;
                        auction.ConfirmationDeadline = DateTime.UtcNow.AddHours(24);

                        if (auction.Product != null)
                        {
                            await notificationManager.CreateAsync(
                                auction.Product.SellerId,
                                "Reserve Price Not Met",
                                $"Your auction #{auction.Id} ended with a highest bid of {winningBid.Amount:N2} EGP, " +
                                $"which is below your reserve price of {auction.ReservePrice:N2} EGP. " +
                                "Do you accept this bid? You have 24 hours to respond. " +
                                "If no response, the auction will be cancelled.");
                        }

                        await unitOfWork.SaveChangesAsync();
                        await tx.CommitAsync();

                        _logger.LogInformation(
                            "Auction {AuctionId} awaiting seller reserve confirmation until {Deadline}",
                            auction.Id, auction.ConfirmationDeadline);

                        continue;
                    }

                    if (winningBid != null)
                    {
                        auction.WinnerUserId = winningBid.UserId;
                        if (auction.Product != null)
                            auction.Product.Status = ProductStatus.Sold;
                    }

                    if (auction.WinnerUserId.HasValue && winningBid != null && auction.Product != null)
                    {
                        await walletManager.SettleAuctionPaymentAsync(
                            winningBid.UserId, auction.Product.SellerId, winningBid.Amount, auction.Id, auction.CreatedByUserId);

                        var auctioneer = await userRepo.GetByIdAsync(auction.CreatedByUserId);
                        if (auctioneer != null)
                        {
                            var fee = winningBid.Amount * FinancialConstants.AuctionAuctioneerFee;
                            await walletManager.CreditPlatformFeeAsync(auctioneer.Id, fee, "Auction", auction.Id);
                        }
                    }

                    if (auction.WinnerUserId == null && winningBid != null)
                    {
                        await walletManager.ReleaseHeldFundsAsync(
                            winningBid.UserId, winningBid.Amount, "Auction", auction.Id);
                    }

                    await unitOfWork.SaveChangesAsync();
                    await tx.CommitAsync();

                    // Non-critical notifications and emails outside transaction
                    if (auction.WinnerUserId.HasValue)
                    {
                        await notificationManager.CreateAsync(auction.WinnerUserId.Value,
                            "Auction Won", $"You won auction #{auction.Id}!");

                        if (auction.Winner != null)
                        {
                            await emailService.SendAsync(
                                auction.Winner.Email,
                                "You won an auction on Sayiad!",
                                $@"<p>Hello {auction.Winner.FullName},</p>
                                   <p>Congratulations! You won the auction for
                                   <strong>{auction.Product?.Title}</strong>.</p>
                                   <p>Winning bid: <strong>{winningBid?.Amount:N2} EGP</strong> — "
                                   + $"deducted from your wallet. The seller receives {FinancialConstants.AuctionFishermanShare:P1} "
                                   + $"({winningBid?.Amount * FinancialConstants.AuctionFishermanShare:N2} EGP).</p>");
                        }
                    }

                    if (auction.Product != null)
                    {
                        var amount = winningBid?.Amount ?? 0;
                        var sellerAmount = amount * FinancialConstants.AuctionFishermanShare;
                        await notificationManager.CreateAsync(auction.Product.SellerId,
                            "Auction Ended",
                            $"Your auction for '{auction.Product.Title}' has ended. " +
                            $"Winning bid: {amount} EGP. You received {sellerAmount:N2} EGP ({FinancialConstants.AuctionFishermanShare:P1} after platform and auctioneer fees).");
                    }

                    _logger.LogInformation(
                        "Auto-closed expired auction {AuctionId}", auction.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to auto-close auction {AuctionId}", auction.Id);
                }
            }

            await CheckExpiredReserveConfirmationsAsync(
                auctionRepo,
                unitOfWork,
                notificationManager,
                walletManager);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auction expiry check failed");
        }
    }

    private async Task CheckExpiredReserveConfirmationsAsync(
        IAuctionRepository auctionRepo,
        IUnitOfWork unitOfWork,
        INotificationManager notificationManager,
        IWalletManager walletManager)
    {
        var expiredConfirmations =
            await auctionRepo.GetExpiredPendingConfirmationsAsync(DateTime.UtcNow);

        foreach (var auction in expiredConfirmations)
        {
            try
            {
                await using var tx = await unitOfWork.BeginTransactionAsync();

                auction.Status = AuctionStatus.Cancelled;
                auction.ConfirmationDeadline = null;

                await ReleaseActiveBidHoldsAsync(auction, walletManager);

                if (auction.Product != null)
                {
                    await notificationManager.CreateAsync(
                        auction.Product.SellerId,
                        "Auction Cancelled",
                        $"Your auction #{auction.Id} was cancelled because you did not respond to the reserve price confirmation within 24 hours. All held funds have been released.");
                }

                await NotifyAllBiddersAuctionCancelledAsync(auction, notificationManager);

                await unitOfWork.SaveChangesAsync();
                await tx.CommitAsync();

                _logger.LogInformation("Reserve confirmation expired for auction {AuctionId}", auction.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to expire reserve confirmation for auction {AuctionId}", auction.Id);
            }
        }
    }

    private static async Task ReleaseActiveBidHoldsAsync(Auction auction, IWalletManager walletManager)
    {
        var heldBids = auction.Bids
            .Where(b => b.BidStatus == BidStatus.Winning)
            .GroupBy(b => b.UserId)
            .Select(g => new { UserId = g.Key, Amount = g.Max(b => b.Amount) });

        foreach (var bid in heldBids)
            await walletManager.ReleaseHeldFundsAsync(bid.UserId, bid.Amount, "Auction", auction.Id);
    }

    private static async Task NotifyAllBiddersAuctionCancelledAsync(
        Auction auction,
        INotificationManager notificationManager)
    {
        foreach (var bidderId in auction.Bids.Select(b => b.UserId).Distinct())
            await notificationManager.CreateAsync(
                bidderId,
                "Auction Cancelled",
                $"Auction #{auction.Id} was cancelled and any held funds have been released.");
    }
}
