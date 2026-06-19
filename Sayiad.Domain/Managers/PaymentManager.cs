using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sayiad.Data.Data;
using Sayiad.Domain.Common;
using Sayiad.Domain.Constants;
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
    private readonly IAuctionRepository _auctionRepo;

    public PaymentManager(
        IPaymentRepository paymentRepo,
        IOrderRepository orderRepo,
        IUnitOfWork unitOfWork,
        ILogger<PaymentManager> logger,
        IWalletManager walletManager,
        IUserRepository userRepo,
        IOptions<AppSettings> settings,
        IAuctionRepository auctionRepo)
    {
        _paymentRepo = paymentRepo;
        _orderRepo = orderRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _walletManager = walletManager;
        _userRepo = userRepo;
        _settings = settings;
        _auctionRepo = auctionRepo;
    }

    public async Task<PaymentResponse> InitiateAsync(int userId, InitiatePaymentRequest request)
    {
        var order = await _orderRepo.GetByIdAsync(request.OrderId, userId)
            ?? throw new KeyNotFoundException("Order not found");

        if (order.BuyerId != userId)
            throw new UnauthorizedAccessException("You can only pay for your own orders");

        if (order.Status != OrderStatus.Pending)
            throw new InvalidOperationException("Cannot initiate payment: order is not in Pending status.");

        var ownsTransaction = _unitOfWork.CurrentTransaction == null;
        var tx = ownsTransaction
            ? await _unitOfWork.BeginTransactionAsync()
            : _unitOfWork.CurrentTransaction!;

        Payment payment = null!;
        try
        {
            payment = new Payment
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
            if (ownsTransaction) await tx.CommitAsync();
        }
        catch
        {
            if (ownsTransaction) await tx.RollbackAsync();
            throw;
        }
        finally
        {
            if (ownsTransaction) await tx.DisposeAsync();
        }

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

        var ownsTransaction = _unitOfWork.CurrentTransaction == null;
        var tx = ownsTransaction
            ? await _unitOfWork.BeginTransactionAsync()
            : _unitOfWork.CurrentTransaction!;

        try
        {
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
            payment.Order.Status = OrderStatus.Paid;
            payment.Order.UpdatedAt = DateTime.UtcNow;

            if (payment.Order.OrderType == OrderType.Auction && payment.Order.AuctionId.HasValue)
            {
                // Auction flow: use SettleAuctionPaymentAsync with 3-way split
                var auctionSellerId = payment.Order.OrderItems.FirstOrDefault()?.SellerId;
                if (auctionSellerId.HasValue && payment.Order.AuctionId.HasValue)
                {
                    var auction = await _auctionRepo.GetByIdAsync(payment.Order.AuctionId.Value);
                    var auctioneerId = auction?.CreatedByUserId ?? 0;

                    await _walletManager.SettleAuctionPaymentAsync(
                        payment.Order.BuyerId, auctionSellerId.Value, payment.Order.TotalPrice, payment.Order.AuctionId.Value, auctioneerId);
                }
            }
            else
            {
                // Standard product flow
                await _walletManager.DeductForOrderAsync(payment.Order.BuyerId, payment.Order.TotalPrice, payment.Order.Id);

                var admin = await _userRepo.GetByEmailAsync(_settings.Value.AdminEmail);
                var adminId = admin?.Id;

                var sellerGroups = payment.Order.OrderItems.GroupBy(i => i.SellerId);
                foreach (var sellerGroup in sellerGroups)
                {
                    var sellerTotal = sellerGroup.Sum(i => i.Subtotal > 0 ? i.Subtotal : i.UnitPrice * i.Quantity);
                    var fee = sellerTotal * FinancialConstants.ProductPlatformFee;

                    // Pass FULL sellerTotal — CreditSellerAsync calculates 95% internally
                    await _walletManager.CreditSellerAsync(sellerGroup.Key, sellerTotal, payment.Order.Id);
                    if (adminId.HasValue)
                        await _walletManager.CreditPlatformFeeAsync(adminId.Value, fee, "Order", payment.Order.Id);
                }
            }

            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new InvalidOperationException(
                    "Payment was already confirmed by another request. Duplicate confirmation prevented.");
            }

            if (ownsTransaction) await tx.CommitAsync();
        }
        catch
        {
            if (ownsTransaction) await tx.RollbackAsync();
            throw;
        }
        finally
        {
            if (ownsTransaction) await tx.DisposeAsync();
        }

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
