using Microsoft.Extensions.Logging;
using Sayiad.Domain.Common;
using Sayiad.Domain.Contracts;
using Sayiad.Domain.Dtos.UserDtos;

namespace Sayiad.Domain.Managers;

public class UserManager : IUserManager
{
    private readonly IUserRepository _userRepo;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<UserManager> _logger;

    public UserManager(IUserRepository userRepo, IFileStorageService fileStorage, ILogger<UserManager> logger)
    {
        _userRepo = userRepo;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<UserResponse> GetProfileAsync(int userId)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found");

        return MapToProfileResponse(user);
    }

    public async Task<UserResponse> UpdateProfileAsync(int userId, UpdateUserRequest request)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found");

        user.FullName = InputSanitizer.Sanitize(request.FullName);
        user.Phone = request.Phone;
        if (request.ProfileImage != null)
            user.ProfileImage = request.ProfileImage;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepo.UpdateAsync(user);
        _logger.LogInformation("Profile updated for user {UserId}", userId);

        return MapToProfileResponse(user);
    }

    public async Task DeleteProfileImageAsync(int userId)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found");

        if (!string.IsNullOrEmpty(user.ProfileImage))
            await _fileStorage.DeleteAsync(user.ProfileImage);

        user.ProfileImage = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user);

        _logger.LogInformation("Profile image deleted for user {UserId}", userId);
    }

    public async Task<PagedResult<UserAdminResponse>> GetAllUsersAsync(PaginationRequest? pagination = null)
    {
        var p = pagination ?? new PaginationRequest();
        var result = await _userRepo.GetAllAsync(p);
        return new PagedResult<UserAdminResponse>
        {
            Items = result.Items.Select(MapToAdminResponse).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }

    public async Task<UserAdminResponse> GetUserByIdAsync(int userId)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found");

        return MapToAdminResponse(user);
    }

    public async Task ToggleUserStatusAsync(int userId)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found");

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user);

        _logger.LogInformation("User {UserId} status toggled to {IsActive}", userId, user.IsActive);
    }

    private static UserResponse MapToProfileResponse(User user) => new(
        user.Id, user.FullName, user.Email, user.Phone,
        user.ProfileImage, user.Role.ToString(), user.IsActive, user.CreatedAt
    );

    private static UserAdminResponse MapToAdminResponse(User user) => new(
        user.Id, user.FullName, user.Email, user.Phone,
        user.ProfileImage, user.Role.ToString(), user.IsActive,
        user.CreatedAt, user.UpdatedAt
    );
}