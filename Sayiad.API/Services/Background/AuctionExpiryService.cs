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
            var notificationManager = scope.ServiceProvider
                .GetRequiredService<INotificationManager>();
            var emailService = scope.ServiceProvider
                .GetRequiredService<IEmailService>();

            var expiredAuctions = await auctionRepo.GetExpiredActiveAsync();

            foreach (var auction in expiredAuctions)
            {
                try
                {
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

                    await auctionRepo.SaveChangesAsync();

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
                                   <p>The seller will be in touch shortly.</p>");
                        }
                    }

                    if (auction.Product != null)
                    {
                        var amount = winningBid?.Amount ?? 0;
                        await notificationManager.CreateAsync(auction.Product.SellerId,
                            "Auction Ended",
                            $"Your auction for '{auction.Product.Title}' has ended. " +
                            $"Winning bid: {amount} EGP.");
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
