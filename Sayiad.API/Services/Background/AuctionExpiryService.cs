using Sayiad.Data.Data;
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

                    if (winningBid != null && winningBid.Amount >= auction.ReservePrice)
                    {
                        auction.WinnerUserId = winningBid.UserId;
                        if (auction.Product != null)
                            auction.Product.Status = ProductStatus.Sold;
                    }

                    if (auction.WinnerUserId.HasValue && winningBid != null && auction.Product != null)
                    {
                        await walletManager.SettleAuctionPaymentAsync(
                            winningBid.UserId, auction.Product.SellerId, winningBid.Amount, auction.Id);

                        var auctioneer = await userRepo.GetByIdAsync(auction.CreatedByUserId);
                        if (auctioneer != null)
                        {
                            var fee = winningBid.Amount * 0.05m;
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
                                   + $"deducted from your wallet. The seller receives 95% "
                                   + $"({winningBid?.Amount * 0.95m:N2} EGP).</p>");
                        }
                    }

                    if (auction.Product != null)
                    {
                        var amount = winningBid?.Amount ?? 0;
                        var sellerAmount = amount * 0.95m;
                        await notificationManager.CreateAsync(auction.Product.SellerId,
                            "Auction Ended",
                            $"Your auction for '{auction.Product.Title}' has ended. " +
                            $"Winning bid: {amount} EGP. You received {sellerAmount:N2} EGP (95% after 5% auctioneer fee).");
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auction expiry check failed");
        }
    }
}
