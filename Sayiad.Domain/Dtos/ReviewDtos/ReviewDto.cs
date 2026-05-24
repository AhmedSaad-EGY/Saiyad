namespace Sayiad.Domain.Dtos.ReviewDtos;

public record ReviewResponse(int Id, int ProductId, int UserId, string UserName, int Rating, string? Comment, DateTime CreatedAt);
public record CreateReviewRequest(int ProductId, int Rating, string? Comment);
public record ProductRatingResponse(int ProductId, double AverageRating, int ReviewCount);
