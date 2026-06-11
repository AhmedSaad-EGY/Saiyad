using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sayiad.Data.Data;
using Sayiad.Domain.Common;
using Sayiad.Domain.Dtos.PaymentDtos;

namespace Sayiad.Domain.Managers;

public class PaymentManager : IPaymentManager
{
    private readonly IPaymentRepository _paymentRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PaymentManager> _logger;
    private readonly IWalletManager _walletManager;
    private readonly IUserRepository _userRepo;
    private readonly IOptions<AppSettings> _settings;

    public PaymentManager(
        IPaymentRepository paymentRepo,
        IOrderRepository orderRepo,
        IUnitOfWork unitOfWork,
        ILogger<PaymentManager> logger,
        IWalletManager walletManager,
        IUserRepository userRepo,
        IOptions<AppSettings> settings)
    {
        _paymentRepo = paymentRepo;
        _orderRepo = orderRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _walletManager = walletManager;
        _userRepo = userRepo;
        _settings = settings;
    }

    public async Task<PaymentResponse> InitiateAsync(int userId, InitiatePaymentRequest request)
    {
        var order = await _orderRepo.GetByIdAsync(request.OrderId, userId)
            ?? throw new KeyNotFoundException("Order not found");

        if (order.BuyerId != userId)
            throw new UnauthorizedAccessException("You can only pay for your own orders");

        if (order.Status != CustomerOrderStatus.Pending)
            throw new InvalidOperationException("Cannot initiate payment: order is not in Pending status.");

        await using var tx = await _unitOfWork.BeginTransactionAsync();

        var payment = new Payment
        {
            OrderId = request.OrderId,
            Amount = order.TotalPrice,
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = PaymentStatus.Pending,
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
        await _unitOfWork.SaveChangesAsync();
        await tx.CommitAsync();

        _logger.LogInformation("Payment initiated: {PaymentId} for order {OrderId}", payment.Id, request.OrderId);
        return MapToResponse(payment);
    }

    public async Task<PaymentResponse> ConfirmAsync(int paymentId, int userId)
    {
        var payment = await _paymentRepo.GetByIdAsync(paymentId)
            ?? throw new KeyNotFoundException("Payment not found");

        if (payment.Order.BuyerId != userId)
            throw new UnauthorizedAccessException("You cannot confirm another user's payment.");

        if (payment.PaymentStatus != PaymentStatus.Pending)
            throw new InvalidOperationException("Cannot confirm payment: current status is not Pending.");

        await using var tx = await _unitOfWork.BeginTransactionAsync();

        payment.PaymentStatus = PaymentStatus.Confirmed;
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

        // Wallet settlement INSIDE transaction: deduct full amount from buyer, credit each seller 95%, admin gets 5%
        var order = await _orderRepo.GetByIdAsync(payment.OrderId, userId);
        if (order != null)
        {
            await _walletManager.DeductForOrderAsync(order.BuyerId, order.TotalPrice, order.Id);

            var admin = await _userRepo.GetByEmailAsync(_settings.Value.AdminEmail);
            var adminId = admin?.Id;

            var sellerGroups = order.OrderItems.GroupBy(i => i.SellerId);
            foreach (var sellerGroup in sellerGroups)
            {
                var sellerTotal = sellerGroup.Sum(i => i.Subtotal > 0 ? i.Subtotal : i.UnitPrice * i.Quantity);
                var fee = sellerTotal * 0.05m;
                var sellerAmount = sellerTotal - fee;

                await _walletManager.CreditSellerAsync(sellerGroup.Key, sellerAmount, order.Id);
                if (adminId.HasValue)
                    await _walletManager.CreditPlatformFeeAsync(adminId.Value, fee, "Order", order.Id);

                // Release the 5% product listing hold on each sold item (one hold per product)
                foreach (var productGroup in sellerGroup.GroupBy(i => i.ProductId))
                {
                    var first = productGroup.First();
                    var holdAmount = first.UnitPrice * 0.05m;
                    await _walletManager.ReleaseHeldFundsAsync(first.SellerId, holdAmount, "Product", productGroup.Key);
                }
            }
        }

        await _paymentRepo.UpdateAsync(payment);
        await _unitOfWork.SaveChangesAsync();
        await tx.CommitAsync();

        _logger.LogInformation("Payment confirmed: {PaymentId}", paymentId);
        return MapToResponse(payment);
    }

    public async Task<IEnumerable<PaymentResponse>> GetOrderPaymentsAsync(int orderId, int userId)
    {
        var payments = await _paymentRepo.GetOrderPaymentsAsync(orderId, userId);
        return payments.Select(MapToResponse);
    }

    private static PaymentResponse MapToResponse(Payment p) => new(
        p.Id, p.OrderId, p.Amount, p.PaymentMethod,
        p.PaymentStatus.ToString(), p.PaidAt, p.CreatedAt,
        p.Transactions.OrderByDescending(t => t.CreatedAt)
            .Select(t => new TransactionResponse(
                t.Id, t.TransactionReference, t.Amount, t.Status, t.CreatedAt))
            .ToList()
    );
}