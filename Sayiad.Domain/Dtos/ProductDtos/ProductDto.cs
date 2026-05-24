
namespace Sayiad.Domain.Dtos.ProductDtos;

public record ProductResponse(
    int Id, string Title, string Description, string Brand,
    ProductCondition Condition, decimal Price, int StockQuantity,
    string Location, bool IsAuctioned, int? AuctionId, ProductStatus Status,
    int SellerId, string SellerName, int CategoryId, string CategoryName,
    string? PrimaryImageUrl, DateTime CreatedAt, DateTime UpdatedAt,
    int? ReviewedByUserId = null, DateTime? ReviewedAt = null, string? RejectionReason = null
);

public record CreateProductRequest(
    string Title, string Description, string Brand,
    ProductCondition Condition, decimal Price, int StockQuantity,
    string Location, int CategoryId
);

public record UpdateProductRequest(
    string Title, string Description, string Brand,
    ProductCondition Condition, decimal Price, int StockQuantity,
    string Location, int CategoryId, ProductStatus Status
);

public record AddProductImageRequest(string ImageUrl, bool IsPrimary);

public record UpdateProductStatusRequest(ProductStatus Status);

public record RejectProductRequest(string Reason);

public record ProductImageResponse(
    int Id,
    int ProductId,
    string ImageUrl,
    bool IsPrimary
);


