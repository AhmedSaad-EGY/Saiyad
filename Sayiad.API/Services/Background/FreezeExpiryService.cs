namespace Sayiad.Api.Services.Background;

public class FreezeExpiryService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FreezeExpiryService> _logger;

    public FreezeExpiryService(IServiceProvider serviceProvider, ILogger<FreezeExpiryService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FreezeExpiryService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(60), stoppingToken);
                await ProcessExpiredFreezesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FreezeExpiryService cycle");
            }
        }

        _logger.LogInformation("FreezeExpiryService stopped");
    }

    private async Task ProcessExpiredFreezesAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var walletRepo = scope.ServiceProvider.GetRequiredService<IWalletRepository>();

        var expired = await walletRepo.GetExpiredFrozenWalletsAsync();

        foreach (var wallet in expired)
        {
            if (ct.IsCancellationRequested) break;

            using var walletScope = _serviceProvider.CreateScope();
            var walletManager = walletScope.ServiceProvider.GetRequiredService<IWalletManager>();
            var unitOfWork = walletScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            try
            {
                var releasedAmount = wallet.HeldBalance;
                await walletManager.ReleaseExpiredFreezeAsync(wallet.UserId);
                await unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Freeze released: Wallet {WalletId}, Amount {Amount}",
                    wallet.Id, releasedAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to release freeze for wallet {WalletId}", wallet.Id);
            }
        }
    }
}
