
namespace Sayiad.Domain.Dtos.OrderDtos;

public record OrderItemResponse(int Id, int ProductId, string ProductTitle, string? ImageUrl, int SellerId, string SellerName, int Quantity, decimal UnitPrice, decimal Subtotal);
public record OrderResponse(int Id, int BuyerId, string BuyerName, decimal TotalPrice, CustomerOrderStatus Status, DateTime CreatedAt, DateTime UpdatedAt, List<OrderItemResponse> Items);
public record CreateOrderRequest(int ShippingAddressId);
public record UpdateOrderStatusRequest(CustomerOrderStatus Status);
