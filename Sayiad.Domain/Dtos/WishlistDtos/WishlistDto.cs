namespace Sayiad.Domain.Dtos.WishlistDtos;

public record WishlistItemResponse(int Id, int ProductId, string ProductTitle, decimal Price, string? ImageUrl, DateTime CreatedAt);
public record ToggleWishlistRequest(int ProductId);
