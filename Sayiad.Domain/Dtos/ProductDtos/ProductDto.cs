
namespace Sayiad.Domain.Dtos.ProductDtos;

public record ProductResponse(
    int Id, string Title, string Description, string Brand,
    ProductCondition Condition, decimal Price, int StockQuantity,
    string Location, bool IsAuctioned, ProductStatus Status,
    int SellerId, string SellerName, int CategoryId, string CategoryName,
    string? PrimaryImageUrl, DateTime CreatedAt, DateTime UpdatedAt
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

public record ProductImageResponse(
    int Id,
    int ProductId,
    string ImageUrl,
    bool IsPrimary
);


