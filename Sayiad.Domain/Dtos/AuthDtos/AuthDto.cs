namespace Sayiad.Domain.Dtos.AuthDtos;

public record RegisterRequest(string FullName, string Email, string Password, string Phone, string? Birthdate = null, string ConfirmPassword = "", string Role = "Customer", string? LicenseNumber = null);
public record LoginRequest(string Email, string Password);
public record RefreshTokenRequest(string RefreshToken);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record AuthResponse(string Token, string RefreshToken, DateTime ExpiresAt, UserInfo User, string? PendingRoleUpgrade = null);
public record UserInfo(int Id, string FullName, string Email, string Phone, string? ProfileImage, string Role, bool IsActive);
