namespace Sayiad.Domain.Dtos.AuthDtos;

public record ResetPasswordRequest(string Email, string Token, string NewPassword, string ConfirmPassword);
