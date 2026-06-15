namespace Sayiad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class AdminController : BaseController
{
    private readonly IWalletRepository _walletRepo;
    private readonly ISystemWalletRepository _systemWalletRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly IReportRepository _reportRepo;

    public AdminController(
        IWalletRepository walletRepo,
        ISystemWalletRepository systemWalletRepo,
        IOrderRepository orderRepo,
        IReportRepository reportRepo)
    {
        _walletRepo = walletRepo;
        _systemWalletRepo = systemWalletRepo;
        _orderRepo = orderRepo;
        _reportRepo = reportRepo;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var systemWallet = await _systemWalletRepo.GetOrThrowAsync();
        var pendingFreezeCount = await _walletRepo.CountExpiredFrozenWalletsAsync();
        var pendingReports = await _reportRepo.GetAllAsync(ReportStatus.Pending, 1, 1);

        return Ok(new
        {
            systemWallet = new
            {
                systemWallet.Balance,
                systemWallet.HeldBalance,
                systemWallet.AvailableBalance
            },
            pendingFreezeCount,
            pendingReportCount = pendingReports.TotalCount
        });
    }

    [HttpGet("system-wallet/transactions")]
    public async Task<IActionResult> GetSystemWalletTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        var transactions = await _systemWalletRepo.GetTransactionsAsync(page, pageSize);
        return Ok(transactions.Select(t => new
        {
            t.Id,
            t.Amount,
            Type = t.Type.ToString(),
            t.ReferenceType,
            t.ReferenceId,
            t.Description,
            t.BalanceSnapshot,
            t.IsFrozen,
            t.CreatedAt
        }));
    }
}
