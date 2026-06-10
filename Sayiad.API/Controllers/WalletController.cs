using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sayiad.Domain.Dtos.WalletDtos;
using Sayiad.Domain.Managers;
using System.Security.Claims;

namespace Sayiad.API.Controllers;

    [Route("api/[controller]")]
[ApiController]
[Authorize]
public class WalletController : ControllerBase
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

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token");
        return int.Parse(claim.Value);
    }
}