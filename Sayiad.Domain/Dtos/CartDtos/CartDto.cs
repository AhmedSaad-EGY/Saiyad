namespace Sayiad.Domain.Dtos.CartDtos;

public record CartItemResponse(int Id, int ProductId, string ProductTitle, decimal Price, string? ImageUrl, int Quantity, decimal Subtotal, DateTime AddedAt);
public record CartResponse(int CartId, List<CartItemResponse> Items, decimal Total);
public record AddToCartRequest(int ProductId, int Quantity);
public record UpdateCartItemRequest(int Quantity);
