using Microsoft.Extensions.Logging;
using Sayiad.Data.Common;
using Sayiad.Data.Data;
using Sayiad.Domain.Constants;
using Sayiad.Domain.Contracts;
using Sayiad.Domain.Dtos.OrderDtos;

namespace Sayiad.Domain.Managers;

public class OrderManager : IOrderManager
{
    private readonly IOrderRepository _orderRepo;
    private readonly IProductRepository _productRepo;
    private readonly ICartRepository _cartRepo;
    private readonly IUserRepository _userRepo;
    private readonly ISellerProfileRepository _sellerProfileRepo;
    private readonly IWalletManager _walletManager;
    private readonly INotificationManager _notificationManager;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderManager> _logger;

    public OrderManager(
        IOrderRepository orderRepo,
        IProductRepository productRepo,
        ICartRepository cartRepo,
        IUserRepository userRepo,
        ISellerProfileRepository sellerProfileRepo,
        IWalletManager walletManager,
        INotificationManager notificationManager,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        ILogger<OrderManager> logger)
    {
        _orderRepo = orderRepo;
        _productRepo = productRepo;
        _cartRepo = cartRepo;
        _userRepo = userRepo;
        _sellerProfileRepo = sellerProfileRepo;
        _walletManager = walletManager;
        _notificationManager = notificationManager;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<OrderResponse> CreateFromCartAsync(int userId, CreateOrderRequest request)
    {
        var cart = await _cartRepo.GetCartAsync(userId)
            ?? throw new InvalidOperationException("Cart is empty");

        if (cart.CartItems.Count == 0)
            throw new InvalidOperationException("Cart is empty");

        _ = await GetShippingAddressAsync(userId, request.ShippingAddressId);

        var productCache = new Dictionary<int, Product>();
        var ownsTransaction = _unitOfWork.CurrentTransaction == null;
        var transaction = ownsTransaction
            ? await _unitOfWork.BeginTransactionAsync()
            : _unitOfWork.CurrentTransaction!;
        Order order = null!;

        try
        {
            foreach (var item in cart.CartItems)
            {
                var product = await _productRepo.GetByIdAsync(item.ProductId)
                    ?? throw new KeyNotFoundException($"Product #{item.ProductId} not found");

                if (product.IsAuctioned)
                    throw new InvalidOperationException(
                        "Auction items cannot be purchased directly. Please bid through the auction.");

                if (product.StockQuantity < item.Quantity)
                    throw new InvalidOperationException(
                        $"Insufficient stock for {item.Product.Title}");

                productCache[item.ProductId] = product;
            }

            order = new Order
            {
                BuyerId = userId,
                ShippingAddressId = request.ShippingAddressId,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            foreach (var cartItem in cart.CartItems)
            {
                var product = productCache[cartItem.ProductId];
                var subtotal = product.Price * cartItem.Quantity;
                order.TotalPrice += subtotal;

                order.OrderItems.Add(new OrderItem
                {
                    ProductId = cartItem.ProductId,
                    SellerId = product.SellerId,
                    Quantity = cartItem.Quantity,
                    UnitPrice = product.Price,
                    Subtotal = subtotal,
                    CreatedAt = DateTime.UtcNow
                });
            }

            order = await _orderRepo.CreateOrderTransactionAsync(order, userId);

            await _unitOfWork.SaveChangesAsync();
            if (ownsTransaction) await transaction.CommitAsync();
        }
        catch
        {
            if (ownsTransaction) await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            if (ownsTransaction) await transaction.DisposeAsync();
        }

        await _notificationManager.CreateAsync(userId, "Order Placed",
            $"Your order #{order.Id} has been placed successfully.");

        var buyer = await _userRepo.GetByIdAsync(userId);
        if (buyer != null)
        {
            await _emailService.SendAsync(
                buyer.Email,
                $"Order #{order.Id} confirmed — Sayiad",
                $@"<p>Hello {buyer.FullName},</p>
                   <p>Your order <strong>#{order.Id}</strong> has been placed successfully.</p>
                   <p>Total: <strong>{order.TotalPrice:N2} EGP</strong></p>
                   <p>We'll notify you when it ships.</p>");
        }

        _logger.LogInformation("Order created: {OrderId} by user {UserId}", order.Id, userId);
        return await GetByIdAsync(order.Id, userId);
    }

    public async Task<PagedResult<OrderResponse>> GetAllOrdersAsync(PaginationRequest? pagination = null)
    {
        var p = pagination ?? new PaginationRequest();
        var result = await _orderRepo.GetAllOrdersAsync(p);
        return new PagedResult<OrderResponse>
        {
            Items = result.Items.Select(MapToResponse).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }

    public async Task<PagedResult<OrderResponse>> GetUserOrdersAsync(int userId, PaginationRequest? pagination = null)
    {
        var p = pagination ?? new PaginationRequest();
        var result = await _orderRepo.GetUserOrdersAsync(userId, p);
        return new PagedResult<OrderResponse>
        {
            Items = result.Items.Select(MapToResponse).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }

    public async Task<IEnumerable<OrderResponse>> GetSellerOrdersAsync(int sellerId)
    {
        var orders = await _orderRepo.GetSellerOrdersAsync(sellerId);
        return orders.Select(MapToResponse);
    }

    public async Task<OrderResponse> GetByIdAsync(int orderId, int userId)
    {
        var order = await _orderRepo.GetByIdAsync(orderId, userId)
            ?? throw new KeyNotFoundException("Order not found");

        return MapToResponse(order);
    }

    public async Task<OrderResponse> CancelAsync(int orderId, int userId)
    {
        var order = await _orderRepo.GetByIdAsync(orderId, userId)
            ?? throw new KeyNotFoundException("Order not found");

        if (order.BuyerId != userId)
            throw new UnauthorizedAccessException("You can only cancel your own orders");

        if (order.Status != OrderStatus.Pending)
            throw new InvalidOperationException("Only pending orders can be cancelled");

        await using var transaction = await _unitOfWork.BeginTransactionAsync();

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;

        foreach (var orderItem in order.OrderItems)
        {
            var product = orderItem.Product;
            if (product != null)
            {
                product.StockQuantity += orderItem.Quantity;
                await _productRepo.UpdateAsync(product);
            }
        }

        await _orderRepo.UpdateAsync(order);
        await _unitOfWork.SaveChangesAsync();
        await transaction.CommitAsync();

        await _notificationManager.CreateAsync(userId, "Order Cancelled",
            $"Your order #{order.Id} has been cancelled.");

        _logger.LogInformation("Order cancelled: {OrderId} by user {UserId}", orderId, userId);
        return MapToResponse(order);
    }

    private static readonly Dictionary<(OrderType, OrderStatus), OrderStatus[]> ValidTransitions = new()
    {
        [(OrderType.Product, OrderStatus.Pending)] = [OrderStatus.Paid, OrderStatus.Cancelled],
        [(OrderType.Product, OrderStatus.Paid)] = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [(OrderType.Product, OrderStatus.Shipped)] = [OrderStatus.Delivered],
        [(OrderType.Auction, OrderStatus.Pending)] = [OrderStatus.Paid],
        [(OrderType.Auction, OrderStatus.Paid)] = [OrderStatus.Delivered],
    };

    public async Task<OrderResponse> UpdateStatusAsync(int orderId, OrderStatus status, int? updatedByUserId = null)
    {
        var order = await _orderRepo.GetByIdForAdminAsync(orderId)
            ?? throw new KeyNotFoundException("Order not found");

        var key = (order.OrderType, order.Status);
        if (!ValidTransitions.TryGetValue(key, out var allowed) || !allowed.Contains(status))
            throw new InvalidOperationException(
                $"Cannot transition {order.OrderType} order from {order.Status} to {status}");

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;

        if (status == OrderStatus.Delivered)
            order.DeliveredAt = DateTime.UtcNow;

        await _orderRepo.UpdateAsync(order);

        if (status == OrderStatus.Delivered)
        {
            foreach (var sellerId in order.OrderItems.Select(oi => oi.SellerId).Distinct())
                await _sellerProfileRepo.IncrementSalesAsync(sellerId);
        }

        await _notificationManager.CreateAsync(order.BuyerId, "Order Updated",
            $"Your order #{order.Id} status changed to {status}.");

        _logger.LogInformation("Order {OrderId} status updated to {Status} by user {UserId}",
            orderId, status, updatedByUserId);
        return MapToResponse(order);
    }

    public async Task<OrderResponse> RequestReturnAsync(int orderId, int userId)
    {
        var order = await _orderRepo.GetByIdAsync(orderId, userId)
            ?? throw new KeyNotFoundException("Order not found");

        if (order.BuyerId != userId)
            throw new UnauthorizedAccessException("You can only request return for your own orders");

        if (order.Status != OrderStatus.Delivered)
            throw new InvalidOperationException("Only delivered orders can be returned");

        if (order.OrderType == OrderType.Auction)
            throw new InvalidOperationException("Auction orders cannot be returned");

        if (!order.DeliveredAt.HasValue ||
            order.DeliveredAt.Value.AddDays(FinancialConstants.ProductFreezeDays) < DateTime.UtcNow)
            throw new InvalidOperationException(
                $"Return window of {FinancialConstants.ProductFreezeDays} days has passed");

        order.ReturnRequested = true;
        order.ReturnRequestedAt = DateTime.UtcNow;
        order.Status = OrderStatus.ReturnRequested;
        order.UpdatedAt = DateTime.UtcNow;
        await _orderRepo.UpdateAsync(order);

        await _notificationManager.CreateAsync(order.BuyerId, "Return Requested",
            $"Your return request for order #{order.Id} has been submitted.");

        _logger.LogInformation("Return requested: Order {OrderId} by user {UserId}", orderId, userId);
        return MapToResponse(order);
    }

    public async Task<OrderResponse> ApproveReturnAsync(int orderId, int adminId)
    {
        var order = await _orderRepo.GetByIdForAdminAsync(orderId)
            ?? throw new KeyNotFoundException("Order not found");

        if (order.Status != OrderStatus.ReturnRequested)
            throw new InvalidOperationException("Order does not have a pending return request");

        if (order.OrderType == OrderType.Auction)
            throw new InvalidOperationException(
                "Auction orders cannot be returned. Delivery is confirmed in person.");

        if (!order.DeliveredAt.HasValue ||
            order.DeliveredAt.Value.AddDays(FinancialConstants.ProductFreezeDays) < DateTime.UtcNow)
            throw new InvalidOperationException("Return window has expired");

        await using var transaction = await _unitOfWork.BeginTransactionAsync();

        // Group order items by seller to reverse each seller's payout
        foreach (var sellerGroup in order.OrderItems.GroupBy(oi => oi.SellerId))
        {
            var sellerTotal = sellerGroup.Sum(oi => oi.Subtotal);
            await _walletManager.ReverseSellerPayoutAsync(sellerGroup.Key, sellerTotal, orderId);
        }

        // Reverse platform fee
        var totalPlatformFee = order.TotalPrice * FinancialConstants.ProductPlatformFee;
        await _walletManager.ReversePlatformFeeAsync(totalPlatformFee, orderId);

        // Refund buyer
        await _walletManager.RefundBuyerAsync(order.BuyerId, order.TotalPrice, orderId);

        // Restore product stock
        foreach (var orderItem in order.OrderItems)
        {
            var product = orderItem.Product;
            if (product != null)
            {
                product.StockQuantity += orderItem.Quantity;
                await _productRepo.UpdateAsync(product);
            }
        }

        order.Status = OrderStatus.Returned;
        order.ReturnRequested = false;
        order.UpdatedAt = DateTime.UtcNow;
        await _orderRepo.UpdateAsync(order);

        await _unitOfWork.SaveChangesAsync();
        await transaction.CommitAsync();

        await _notificationManager.CreateAsync(order.BuyerId, "Return Approved",
            $"Your return for order #{order.Id} has been approved. The refund will be credited to your wallet.");

        _logger.LogInformation("Return approved: Order {OrderId} by admin {AdminId}", orderId, adminId);
        return MapToResponse(order);
    }

    public async Task<OrderResponse> RejectReturnAsync(int orderId, int adminId, string reason)
    {
        var order = await _orderRepo.GetByIdForAdminAsync(orderId)
            ?? throw new KeyNotFoundException("Order not found");

        if (order.Status != OrderStatus.ReturnRequested)
            throw new InvalidOperationException("Order does not have a pending return request");

        order.ReturnRequested = false;
        order.ReturnRequestedAt = null;
        order.ReturnReason = null;
        order.Status = OrderStatus.Delivered;
        order.UpdatedAt = DateTime.UtcNow;
        await _orderRepo.UpdateAsync(order);

        await _notificationManager.CreateAsync(order.BuyerId, "Return Rejected",
            $"Your return request for order #{order.Id} has been rejected. Reason: {reason}");

        _logger.LogInformation("Return rejected: Order {OrderId} by admin {AdminId}, reason: {Reason}",
            orderId, adminId, reason);
        return MapToResponse(order);
    }

    private async Task<ShippingAddress> GetShippingAddressAsync(int userId, int addressId)
    {
        return await _orderRepo.GetShippingAddressAsync(addressId, userId)
            ?? throw new KeyNotFoundException("Shipping address not found");
    }

    private static OrderResponse MapToResponse(Order order)
    {
        var items = order.OrderItems.Select(oi => new OrderItemResponse(
            oi.Id, oi.ProductId, oi.Product.Title,
            oi.Product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
            oi.SellerId, oi.Seller.FullName,
            oi.Quantity, oi.UnitPrice, oi.Subtotal
        )).ToList();

        return new OrderResponse(
            order.Id, order.BuyerId, order.Buyer.FullName,
            order.TotalPrice, order.Status,
            order.CreatedAt, order.UpdatedAt,
            order.DeliveredAt, order.OrderType.ToString(),
            order.ReturnRequested, order.ReturnRequestedAt, order.ReturnReason, items);
    }
}
