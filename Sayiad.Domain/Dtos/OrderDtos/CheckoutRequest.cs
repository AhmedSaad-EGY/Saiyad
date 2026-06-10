namespace Sayiad.Domain.Dtos.OrderDtos;

public record CheckoutRequest(
    int ShippingAddressId,
    string PaymentMethod
);

public record CheckoutResponse(
    int OrderId,
    string Status,
    decimal TotalAmount
);
