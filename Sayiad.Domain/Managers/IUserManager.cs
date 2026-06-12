using Sayiad.Domain.Dtos.UserDtos;

namespace Sayiad.Domain.Managers;

public interface IUserManager
{
    Task<UserResponse> GetProfileAsync(int userId);
    Task<UserResponse> UpdateProfileAsync(int userId, UpdateUserRequest request);
    Task DeleteProfileImageAsync(int userId);
    Task<PagedResult<UserAdminResponse>> GetAllUsersAsync(PaginationRequest? pagination = null);
    Task<UserAdminResponse> GetUserByIdAsync(int userId);
    Task ToggleUserStatusAsync(int userId);
    Task ApproveRoleRequestAsync(int userId);
    Task RejectRoleRequestAsync(int userId, string? reason);
}
