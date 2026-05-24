namespace Sayiad.Data.Models;

public record AuctionFilterRequest(
    string? SearchTerm = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? Status = null
);
