namespace Sayiad.Domain.Dtos.UserDtos;

public record UserResponse(
    int Id,
    string FullName,
    string Email,
    string Phone,
    string? ProfileImage,
    string Role,
    bool IsActive,
    DateTime CreatedAt
    );

public record UpdateUserRequest(
    string FullName,
    string Phone,
    string? ProfileImage
);

public record UserAdminResponse(
    int Id,
    string FullName,
    string Email,
    string Phone,
    string? ProfileImage,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
