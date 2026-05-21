namespace Sayiad.Data.Models;

public record ProductFilterRequest(
    int? CategoryId,
    decimal? MinPrice,
    decimal? MaxPrice,
    ProductCondition? Condition,
    string? Location,
    string? SearchTerm,
    bool? InStock = null,
    string? SortBy = null,
    string? SortDirection = null
);
