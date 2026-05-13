using Sayiad.Domain.Dtos.SellerProfileDtos;

namespace Sayiad.Domain.Managers;

public class SellerProfileManager : ISellerProfileManager
{
    private readonly ISellerProfileRepository _repo;
    private readonly IProductRepository _productRepo;
    private readonly IOrderRepository _orderRepo;

    public SellerProfileManager(
        ISellerProfileRepository repo,
        IProductRepository productRepo,
        IOrderRepository orderRepo)
    {
        _repo = repo;
        _productRepo = productRepo;
        _orderRepo = orderRepo;
    }

    public async Task<SellerProfileResponse> CreateAsync(int userId, CreateSellerProfileRequest request)
    {
        var existing = await _repo.GetByUserIdAsync(userId);
        if (existing != null)
            throw new InvalidOperationException("Seller profile already exists");

        var profile = new SellerProfile
        {
            UserId = userId,
            StoreName = request.StoreName,
            StoreDescription = request.Description ?? string.Empty,
            AverageRating = 0,
            TotalSales = 0,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repo.CreateAsync(profile);
        return MapToResponse(created);
    }

    public async Task<SellerProfileResponse> UpdateAsync(int userId, UpdateSellerProfileRequest request)
    {
        var profile = await _repo.GetByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("Seller profile not found");

        if (profile.UserId != userId)
            throw new UnauthorizedAccessException("You can only update your own profile.");

        profile.StoreName = request.StoreName;
        profile.StoreDescription = request.Description ?? string.Empty;

        var updated = await _repo.UpdateAsync(profile);
        return MapToResponse(updated);
    }

    public async Task<SellerProfileResponse> GetByUserIdAsync(int userId)
    {
        var profile = await _repo.GetByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("Seller profile not found");

        return MapToResponse(profile);
    }

    public async Task<SellerProfileResponse> GetMyProfileAsync(int userId)
    {
        var profile = await _repo.GetByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("Seller profile not found");

        return MapToResponse(profile);
    }

    public async Task<SellerDashboardResponse> GetDashboardAsync(int userId)
    {
        var profile = await _repo.GetByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("Seller profile not found");

        var products = await _productRepo.GetSellerProductsAsync(userId);
        var totalProducts = products.Count();

        var orders = await _orderRepo.GetSellerOrdersAsync(userId);
        var recentOrders = orders
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .Select(o => new DashboardOrderItem(
                o.Id, o.Buyer.FullName, o.TotalPrice,
                o.Status.ToString(), o.CreatedAt))
            .ToList();

        return new SellerDashboardResponse(
            profile.StoreName, profile.StoreDescription,
            (double)profile.AverageRating, profile.TotalSales,
            totalProducts, recentOrders);
    }

    private static SellerProfileResponse MapToResponse(SellerProfile p)
    {
        return new SellerProfileResponse(
            p.Id, p.UserId, p.User.FullName, p.StoreName,
            p.StoreDescription, null, null, null,
            (double)p.AverageRating, p.TotalSales
        );
    }
}
