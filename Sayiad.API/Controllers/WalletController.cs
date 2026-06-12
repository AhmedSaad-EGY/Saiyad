namespace Sayiad.Api.Controllers;

    [Route("api/[controller]")]
[ApiController]
[Authorize]
public class WalletController : BaseController
{
    private readonly IWalletManager _walletManager;

    public WalletController(IWalletManager walletManager)
    {
        _walletManager = walletManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetWallet()
    {
        var userId = GetUserId();
        var wallet = await _walletManager.GetWalletAsync(userId);
        return Ok(wallet);
    }

    [Authorize]
    [HttpPost("deposit")]
    public async Task<IActionResult> Deposit([FromBody] DepositRequest request)
    {
        var userId = GetUserId();
        var wallet = await _walletManager.DepositAsync(userId, request.Amount);
        return Ok(wallet);
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions([FromQuery] PaginationRequest pagination)
    {
        var userId = GetUserId();
        var transactions = await _walletManager.GetTransactionsAsync(userId, pagination);
        return Ok(transactions);
    }
}