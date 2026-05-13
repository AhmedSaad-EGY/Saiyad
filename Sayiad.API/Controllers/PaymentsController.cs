using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sayiad.Domain.Dtos.PaymentDtos;

namespace Sayiad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentManager _paymentManager;

    public PaymentsController(IPaymentManager paymentManager)
    {
        _paymentManager = paymentManager;
    }

    [HttpPost("initiate")]
    public async Task<IActionResult> Initiate(InitiatePaymentRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _paymentManager.InitiateAsync(userId, request);
        return Created("", result);
    }

    [HttpPost("{paymentId}/confirm")]
    public async Task<IActionResult> Confirm(int paymentId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _paymentManager.ConfirmAsync(paymentId, userId);
        return Ok(result);
    }

    [HttpGet("order/{orderId}")]
    public async Task<IActionResult> GetOrderPayments(int orderId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var payments = await _paymentManager.GetOrderPaymentsAsync(orderId, userId);
        return Ok(payments);
    }
}
