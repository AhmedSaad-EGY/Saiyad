namespace Sayiad.Domain.Dtos.AuthDtos;

public record VerifyResetCodeRequest(string Email, string Token);
