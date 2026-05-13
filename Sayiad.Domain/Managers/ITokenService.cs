namespace Sayiad.Domain.Managers;

public interface ITokenService
{
    (string Token, DateTime Expiry) GenerateJwtToken(User user);
    string GenerateRefreshToken();
}