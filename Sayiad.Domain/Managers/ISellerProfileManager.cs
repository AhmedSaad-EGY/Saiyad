using Sayiad.Domain.Dtos.SellerProfileDtos;

namespace Sayiad.Domain.Managers;

public interface ISellerProfileManager
{
    Task<SellerProfileResponse> CreateAsync(int userId, CreateSellerProfileRequest request);
    Task<SellerProfileResponse> UpdateAsync(int userId, UpdateSellerProfileRequest request);
    Task<SellerProfileResponse> GetByUserIdAsync(int userId);
    Task<SellerProfileResponse> GetMyProfileAsync(int userId);
    Task<SellerDashboardResponse> GetDashboardAsync(int userId);
}
