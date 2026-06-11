using Microsoft.Extensions.Logging;
using Sayiad.Data.Data;
using Sayiad.Domain.Common;
using Sayiad.Domain.Dtos.ReviewDtos;

namespace Sayiad.Domain.Managers;

public class ReviewManager : IReviewManager
{
    private readonly IReviewRepository _reviewRepo;
    private readonly IProductRepository _productRepo;
    private readonly ISellerProfileRepository _sellerProfileRepo;
    private readonly INotificationManager _notificationManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReviewManager> _logger;

    public ReviewManager(IReviewRepository reviewRepo, IProductRepository productRepo, ISellerProfileRepository sellerProfileRepo, INotificationManager notificationManager, IUnitOfWork unitOfWork, ILogger<ReviewManager> logger)
    {
        _reviewRepo = reviewRepo;
        _productRepo = productRepo;
        _sellerProfileRepo = sellerProfileRepo;
        _notificationManager = notificationManager;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<ReviewResponse>> GetProductReviewsAsync(int productId)
    {
        var reviews = await _reviewRepo.GetProductReviewsAsync(productId);
        return reviews.Select(r => new ReviewResponse(
            r.Id, r.ProductId, r.UserId, r.User.FullName,
            r.Rating, r.Comment, r.CreatedAt));
    }

    public async Task<ProductRatingResponse> GetProductRatingAsync(int productId)
    {
        var reviews = await _reviewRepo.GetProductReviewsAsync(productId);
        var list = reviews.ToList();
        return new ProductRatingResponse(
            productId,
            list.Any() ? list.Average(r => r.Rating) : 0,
            list.Count);
    }

    public async Task<ReviewResponse> CreateAsync(int userId, CreateReviewRequest request)
    {
        var exists = await _productRepo.ExistsAsync(request.ProductId);
        if (!exists)
            throw new KeyNotFoundException("Product not found");

        var existing = await _reviewRepo.ExistsForUserAsync(request.ProductId, userId);
        if (existing)
            throw new InvalidOperationException("You already reviewed this product");

        var review = new Review
        {
            ProductId = request.ProductId,
            UserId = userId,
            Rating = request.Rating,
            Comment = InputSanitizer.SanitizeNullable(request.Comment),
            CreatedAt = DateTime.UtcNow
        };

        await using var tx = await _unitOfWork.BeginTransactionAsync();
        await _reviewRepo.AddAsync(review);
        await _unitOfWork.SaveChangesAsync();
        await tx.CommitAsync();

        _logger.LogInformation("Review created: Product {ProductId}, User {UserId}, Rating {Rating}",
            request.ProductId, userId, request.Rating);

        var savedReview = await _reviewRepo.GetByIdAsync(review.Id, userId)
            ?? throw new KeyNotFoundException("Review not found");

        var product = await _productRepo.GetByIdAsync(request.ProductId);
        if (product != null)
        {
            await _sellerProfileRepo.UpdateRatingAsync(product.SellerId);
            await _notificationManager.CreateAsync(product.SellerId, "New Review",
                $"Your product received a new {request.Rating}-star review.");
        }

        return new ReviewResponse(
            savedReview.Id, savedReview.ProductId, savedReview.UserId, savedReview.User.FullName,
            savedReview.Rating, savedReview.Comment, savedReview.CreatedAt);
    }

    public async Task DeleteAsync(int reviewId, int userId)
    {
        var review = await _reviewRepo.GetByIdAsync(reviewId, userId)
            ?? throw new KeyNotFoundException("Review not found");

        if (review.UserId != userId)
            throw new UnauthorizedAccessException("You can only delete your own reviews");

        await _reviewRepo.DeleteAsync(review);
    }
}