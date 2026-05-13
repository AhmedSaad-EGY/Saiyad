namespace Sayiad.Domain.Dtos.AuthDtos;

public record RegisterRequest(string FullName, string Email, string Password, string Phone, string Role = "Customer");
public record LoginRequest(string Email, string Password);
public record RefreshTokenRequest(string RefreshToken);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record AuthResponse(string Token, string RefreshToken, DateTime ExpiresAt, UserInfo User);
public record UserInfo(int Id, string FullName, string Email, string Phone, string? ProfileImage, string Role, bool IsActive);
