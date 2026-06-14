namespace Sayiad.Api.Services.Background;

public class ReturnExpiryService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReturnExpiryService> _logger;

    public ReturnExpiryService(IServiceProvider serviceProvider, ILogger<ReturnExpiryService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReturnExpiryService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(60), stoppingToken);
                await ProcessExpiredReturnsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ReturnExpiryService cycle");
            }
        }

        _logger.LogInformation("ReturnExpiryService stopped");
    }

    private async Task ProcessExpiredReturnsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var notificationManager = scope.ServiceProvider.GetRequiredService<INotificationManager>();

        var cutoff = DateTime.UtcNow.AddDays(-Sayiad.Domain.Constants.FinancialConstants.ProductFreezeDays);
        var expired = await orderRepo.GetPendingReturnRequestsAsync(cutoff);

        foreach (var order in expired)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                order.ReturnRequested = false;
                order.ReturnRequestedAt = null;
                order.ReturnReason = null;
                order.Status = OrderStatus.Delivered;
                order.UpdatedAt = DateTime.UtcNow;

                await notificationManager.CreateAsync(order.BuyerId, "Return Expired",
                    $"The return window for order #{order.Id} has expired. The return request has been automatically closed.");
                await unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Return auto-rejected: Order {OrderId}, Buyer {BuyerId}",
                    order.Id, order.BuyerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-reject return for order {OrderId}", order.Id);
            }
        }
    }
}
