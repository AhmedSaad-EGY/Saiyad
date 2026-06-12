namespace Sayiad.Api.Controllers;

[ApiController]
    [Route("api/[controller]")]
[Authorize(Roles = $"{nameof(UserRole.Customer)},{nameof(UserRole.Fisherman)},{nameof(UserRole.BaitSeller)},{nameof(UserRole.Auctioneer)}")]
public class PaymentsController : BaseController
{
    private readonly IPaymentManager _paymentManager;

    public PaymentsController(IPaymentManager paymentManager)
    {
        _paymentManager = paymentManager;
    }

    [HttpPost("initiate")]
    public async Task<IActionResult> Initiate(InitiatePaymentRequest request)
    {
        var userId = GetUserId();
        var result = await _paymentManager.InitiateAsync(userId, request);
        return Created("", result);
    }

    [HttpPost("{paymentId}/confirm")]
    public async Task<IActionResult> Confirm(int paymentId)
    {
        var userId = GetUserId();
        var result = await _paymentManager.ConfirmAsync(paymentId, userId);
        return Ok(result);
    }

    [HttpGet("order/{orderId}")]
    public async Task<IActionResult> GetOrderPayments(int orderId)
    {
        var userId = GetUserId();
        var payments = await _paymentManager.GetOrderPaymentsAsync(orderId, userId);
        return Ok(payments);
    }
}
