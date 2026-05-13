using Microsoft.Extensions.Logging;
using Sayiad.Domain.Contracts;
using Sayiad.Domain.Dtos.AuthDtos;

namespace Sayiad.Domain.Managers;

public class AuthManager : IAuthManager
{
    private readonly IUserRepository _userRepo;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthManager> _logger;

    public AuthManager(
        IUserRepository userRepo,
        ITokenService tokenService,
        IEmailService emailService,
        ILogger<AuthManager> logger)
    {
        _userRepo = userRepo;
        _tokenService = tokenService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (request.Role == nameof(UserRole.Admin))
            throw new UnauthorizedAccessException("Admin accounts cannot be self-registered.");

        if (await _userRepo.EmailExistsAsync(request.Email))
            throw new InvalidOperationException("Email already registered");

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Phone = request.Phone,
            Role = Enum.Parse<UserRole>(request.Role),
            IsActive = true,
            IsEmailVerified = false,
            EmailVerificationToken = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepo.AddAsync(user);

        var verifyUrl = $"https://sayiad.runasp.net/api/auth/verify-email?token={user.EmailVerificationToken}";
        await _emailService.SendAsync(
            user.Email,
            "Verify your Sayiad account",
            $@"<p>Hello {user.FullName},</p>
               <p>Please verify your email to activate your Sayiad account:</p>
               <p><a href='{verifyUrl}' style='background:#1a7f5a;color:#fff;padding:10px 20px;
               border-radius:6px;text-decoration:none;'>Verify Email</a></p>
               <p>If you did not register, ignore this email.</p>");

        _logger.LogInformation("User registered: {Email} as {Role}", user.Email, user.Role);
        return await GenerateAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepo.GetByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Invalid email or password");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is disabled");

        if (!user.IsEmailVerified)
            throw new UnauthorizedAccessException("Please verify your email before logging in.");

        _logger.LogInformation("User logged in: {Email}", user.Email);
        return await GenerateAuthResponse(user);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        var user = await _userRepo.GetByRefreshTokenAsync(refreshToken)
            ?? throw new UnauthorizedAccessException("Invalid or expired refresh token");

        _logger.LogInformation("Token refreshed for user: {Email}", user.Email);
        return await GenerateAuthResponse(user);
    }

    public async Task LogoutAsync(int userId)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found");

        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        await _userRepo.UpdateAsync(user);

        _logger.LogInformation("User logged out: {UserId}", userId);
    }

    public async Task VerifyEmailAsync(string token)
    {
        var user = await _userRepo.GetByVerificationTokenAsync(token)
            ?? throw new KeyNotFoundException("Invalid or expired verification token.");

        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user);

        _logger.LogInformation("Email verified for user: {UserId}", user.Id);
    }

    public async Task ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found");

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user);

        await _emailService.SendAsync(
            user.Email,
            "Your Sayiad password was changed",
            $@"<p>Hello {user.FullName},</p>
               <p>Your account password was changed successfully.</p>
               <p>If you did not make this change, contact support immediately.</p>");

        _logger.LogInformation("Password changed for user: {UserId}", userId);
    }

    private async Task<AuthResponse> GenerateAuthResponse(User user)
    {
        var (token, expiry) = _tokenService.GenerateJwtToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _userRepo.UpdateAsync(user);

        return new AuthResponse(
            Token: token,
            RefreshToken: refreshToken,
            ExpiresAt: expiry,
            User: MapUser(user)
        );
    }

    private static UserInfo MapUser(User user) => new(
        user.Id,
        user.FullName,
        user.Email,
        user.Phone,
        user.ProfileImage,
        user.Role.ToString(),
        user.IsActive
    );
}