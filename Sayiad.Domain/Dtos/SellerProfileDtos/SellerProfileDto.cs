namespace Sayiad.Domain.Dtos.SellerProfileDtos;

public record CreateSellerProfileRequest(
    string StoreName,
    string? Description,
    string? ContactEmail,
    string? ContactPhone,
    string? Location
);

public record UpdateSellerProfileRequest(
    string StoreName,
    string? Description,
    string? ContactEmail,
    string? ContactPhone,
    string? Location
);

public record SellerProfileResponse(
    int Id,
    int UserId,
    string SellerName,
    string StoreName,
    string? Description,
    string? ContactEmail,
    string? ContactPhone,
    string? Location,
    double AverageRating,
    int TotalSales
);

public record PublicSellerProfileResponse(
    int UserId,
    string SellerName,
    string? Description,
    string? AvatarUrl,
    double Rating,
    int TotalProducts,
    DateTime MemberSince
);

public record DashboardOrderItem(
    int OrderId,
    string BuyerName,
    decimal TotalPrice,
    string Status,
    DateTime CreatedAt
);

public record SellerDashboardResponse(
    string StoreName,
    string? Description,
    double AverageRating,
    int TotalSales,
    int TotalProducts,
    List<DashboardOrderItem> RecentOrders
);
