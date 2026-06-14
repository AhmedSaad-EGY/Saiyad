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
}
