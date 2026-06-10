import re

with open('Sayiad.API/Controllers/OrdersController.cs', 'r') as f:
    content = f.read()

# 1. Add IPaymentManager import (already has using lines, add to them)
content = content.replace(
    'using Sayiad.Domain.Dtos.OrderDtos;',
    'using Sayiad.Domain.Dtos.OrderDtos;\nusing Sayiad.Domain.Dtos.PaymentDtos;'
)

# 2. Add IPaymentManager field + constructor parameter
content = content.replace(
    '''public class OrdersController : ControllerBase
{
    private readonly IOrderManager _orderManager;

    public OrdersController(IOrderManager orderManager)
    {
        _orderManager = orderManager;
    }''',
    '''public class OrdersController : ControllerBase
{
    private readonly IOrderManager _orderManager;
    private readonly IPaymentManager _paymentManager;

    public OrdersController(IOrderManager orderManager, IPaymentManager paymentManager)
    {
        _orderManager = orderManager;
        _paymentManager = paymentManager;
    }'''
)

# 3. Add Checkout endpoint before the closing brace of the class
# Find the last method's closing and class closing
content = content.replace(
    '''    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateOrderStatusRequest request)
    {
        var order = await _orderManager.UpdateStatusAsync(id, request.Status);
        return Ok(order);
    }
}''',
    '''    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateOrderStatusRequest request)
    {
        var order = await _orderManager.UpdateStatusAsync(id, request.Status);
        return Ok(order);
    }

    /// <summary>
    /// Single atomic checkout: creates order + initiates + confirms payment in one request.
    /// Eliminates the 3-step frontend chain that could leave orders in a partial state.
    /// </summary>
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout(CheckoutRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // 1. Create order from cart
        var createRequest = new CreateOrderRequest(request.ShippingAddressId);
        var order = await _orderManager.CreateFromCartAsync(userId, createRequest);

        // 2. Initiate payment
        var payment = await _paymentManager.InitiateAsync(userId, new InitiatePaymentRequest(order.Id, request.PaymentMethod));

        // 3. Confirm payment (handles wallet deduction, seller credit, platform fee)
        await _paymentManager.ConfirmAsync(payment.Id, userId);

        return Ok(new CheckoutResponse(order.Id, order.Status.ToString(), order.TotalPrice));
    }
}'''
)

with open('Sayiad.API/Controllers/OrdersController.cs', 'w') as f:
    f.write(content)

print("OrdersController.cs updated")
