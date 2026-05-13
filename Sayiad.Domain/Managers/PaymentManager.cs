using Microsoft.Extensions.Logging;
using Sayiad.Domain.Dtos.PaymentDtos;

namespace Sayiad.Domain.Managers;

public class PaymentManager : IPaymentManager
{
    private readonly IPaymentRepository _paymentRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly ILogger<PaymentManager> _logger;

    public PaymentManager(
        IPaymentRepository paymentRepo,
        IOrderRepository orderRepo,
        ILogger<PaymentManager> logger)
    {
        _paymentRepo = paymentRepo;
        _orderRepo = orderRepo;
        _logger = logger;
    }

    public async Task<PaymentResponse> InitiateAsync(int userId, InitiatePaymentRequest request)
    {
        var order = await _orderRepo.GetByIdAsync(request.OrderId)
            ?? throw new KeyNotFoundException("Order not found");

        if (order.BuyerId != userId)
            throw new UnauthorizedAccessException("You can only pay for your own orders");

        if (order.Status != CustomerOrderStatus.Pending)
            throw new InvalidOperationException("Order is not pending payment");

        var payment = new Payment
        {
            OrderId = request.OrderId,
            Amount = order.TotalPrice,
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        var transaction = new Transaction
        {
            TransactionReference = $"TXN-{Guid.NewGuid():N}"[..20],
            Amount = order.TotalPrice,
            Status = "Initiated",
            CreatedAt = DateTime.UtcNow
        };

        payment.Transactions.Add(transaction);
        await _paymentRepo.AddAsync(payment);

        _logger.LogInformation("Payment initiated: {PaymentId} for order {OrderId}", payment.Id, request.OrderId);
        return MapToResponse(payment);
    }

    public async Task<PaymentResponse> ConfirmAsync(int paymentId, int userId)
    {
        var payment = await _paymentRepo.GetByIdAsync(paymentId)
            ?? throw new KeyNotFoundException("Payment not found");

        if (payment.Order.BuyerId != userId)
            throw new UnauthorizedAccessException("You cannot confirm another user's payment.");

        if (payment.PaymentStatus != "Pending")
            throw new InvalidOperationException("Payment is already processed");

        payment.PaymentStatus = "Paid";
        payment.PaidAt = DateTime.UtcNow;

        var transaction = new Transaction
        {
            TransactionReference = $"TXN-{Guid.NewGuid():N}"[..20],
            Amount = payment.Amount,
            Status = "Completed",
            CreatedAt = DateTime.UtcNow
        };

        payment.Transactions.Add(transaction);
        payment.Order.Status = CustomerOrderStatus.Paid;
        payment.Order.UpdatedAt = DateTime.UtcNow;

        await _paymentRepo.UpdateAsync(payment);

        _logger.LogInformation("Payment confirmed: {PaymentId}", paymentId);
        return MapToResponse(payment);
    }

    public async Task<IEnumerable<PaymentResponse>> GetOrderPaymentsAsync(int orderId, int userId)
    {
        var payments = await _paymentRepo.GetOrderPaymentsAsync(orderId);
        return payments.Where(p => p.Order.BuyerId == userId).Select(MapToResponse);
    }

    private static PaymentResponse MapToResponse(Payment p) => new(
        p.Id, p.OrderId, p.Amount, p.PaymentMethod,
        p.PaymentStatus, p.PaidAt, p.CreatedAt,
        p.Transactions.OrderByDescending(t => t.CreatedAt)
            .Select(t => new TransactionResponse(
                t.Id, t.TransactionReference, t.Amount, t.Status, t.CreatedAt))
            .ToList()
    );
}