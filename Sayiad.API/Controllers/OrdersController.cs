
namespace Sayiad.Api.Controllers;

[ApiController]
    [Route("api/[controller]")]
[Authorize(Roles = $"{nameof(UserRole.Customer)},{nameof(UserRole.Fisherman)},{nameof(UserRole.BaitSeller)},{nameof(UserRole.Auctioneer)},{nameof(UserRole.Admin)}")]
public class OrdersController : BaseController
{
    private readonly IOrderManager _orderManager;
    private readonly IPaymentManager _paymentManager;
    private readonly IUnitOfWork _unitOfWork;

    public OrdersController(
        IOrderManager orderManager,
        IPaymentManager paymentManager,
        IUnitOfWork unitOfWork)
    {
        _orderManager = orderManager;
        _paymentManager = paymentManager;
        _unitOfWork = unitOfWork;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderRequest request)
    {
        var userId = GetUserId();
        var order = await _orderManager.CreateFromCartAsync(userId, request);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyOrders([FromQuery] PaginationRequest? pagination)
    {
        var userId = GetUserId();
        var orders = await _orderManager.GetUserOrdersAsync(userId, pagination);
        return Ok(orders);
    }

    [HttpGet("seller")]
    public async Task<IActionResult> GetSellerOrders()
    {
        var userId = GetUserId();
        var orders = await _orderManager.GetSellerOrdersAsync(userId);
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetUserId();
        var order = await _orderManager.GetByIdAsync(id, userId);
        return Ok(order);
    }

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = GetUserId();
        var order = await _orderManager.CancelAsync(id, userId);
        return Ok(order);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("admin")]
    public async Task<IActionResult> GetAllOrders([FromQuery] PaginationRequest? pagination)
    {
        var orders = await _orderManager.GetAllOrdersAsync(pagination);
        return Ok(orders);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateOrderStatusRequest request)
    {
        var adminId = GetUserId();
        var order = await _orderManager.UpdateStatusAsync(id, request.Status, adminId);
        return Ok(order);
    }

    [HttpPost("{id}/request-return")]
    public async Task<IActionResult> RequestReturn(int id)
    {
        var userId = GetUserId();
        var order = await _orderManager.RequestReturnAsync(id, userId);
        return Ok(order);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPut("{id}/approve-return")]
    public async Task<IActionResult> ApproveReturn(int id)
    {
        var adminId = GetUserId();
        var order = await _orderManager.ApproveReturnAsync(id, adminId);
        return Ok(order);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPut("{id}/reject-return")]
    public async Task<IActionResult> RejectReturn(int id, RejectReturnRequest request)
    {
        var adminId = GetUserId();
        var order = await _orderManager.RejectReturnAsync(id, adminId, request.Reason);
        return Ok(order);
    }

    /// <summary>
    /// Single atomic checkout: creates order + initiates + confirms payment in one request.
    /// Eliminates the 3-step frontend chain that could leave orders in a partial state.
    /// </summary>
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout(CheckoutRequest request)
    {
        var userId = GetUserId();

        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            // 1. Create order from cart
            var createRequest = new CreateOrderRequest(request.ShippingAddressId);
            var order = await _orderManager.CreateFromCartAsync(userId, createRequest);

            // 2. Initiate payment
            var payment = await _paymentManager.InitiateAsync(userId, new InitiatePaymentRequest(order.Id, request.PaymentMethod));

            // 3. Confirm payment (handles wallet deduction, seller credit, platform fee)
            await _paymentManager.ConfirmAsync(payment.Id, userId);

            await transaction.CommitAsync();
            return Ok(new CheckoutResponse(order.Id, "Confirmed", order.TotalPrice));
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
