using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sayiad.Domain.Dtos.OrderDtos;

namespace Sayiad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderManager _orderManager;

    public OrdersController(IOrderManager orderManager)
    {
        _orderManager = orderManager;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await _orderManager.CreateFromCartAsync(userId, request);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyOrders([FromQuery] PaginationRequest? pagination)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var orders = await _orderManager.GetUserOrdersAsync(userId, pagination);
        return Ok(orders);
    }

    [HttpGet("seller")]
    public async Task<IActionResult> GetSellerOrders()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var orders = await _orderManager.GetSellerOrdersAsync(userId);
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await _orderManager.GetByIdAsync(id, userId);
        return Ok(order);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateOrderStatusRequest request)
    {
        var order = await _orderManager.UpdateStatusAsync(id, request.Status);
        return Ok(order);
    }
}
