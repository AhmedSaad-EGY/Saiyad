using Microsoft.Extensions.Logging;
using Sayiad.Domain.Common;
using Sayiad.Domain.Contracts;
using Sayiad.Domain.Dtos.AuthDtos;

namespace Sayiad.Domain.Managers;

public class AuthManager : IAuthManager
{
    private readonly IUserRepository _userRepo;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IWalletManager _walletManager;
    private readonly IAuditService _auditService;
    private readonly ILogger<AuthManager> _logger;

    public AuthManager(
        IUserRepository userRepo,
        ITokenService tokenService,
        IEmailService emailService,
        IWalletManager walletManager,
        IAuditService auditService,
        ILogger<AuthManager> logger)
    {
        _userRepo = userRepo;
        _tokenService = tokenService;
        _emailService = emailService;
        _walletManager = walletManager;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // B-002: Override any client-provided role — public registrations are always Customer
        const UserRole fixedRole = UserRole.Customer;

        if (await _userRepo.EmailExistsAsync(request.Email))
            throw new InvalidOperationException("Email already registered");

        var user = new User
        {
            FullName = InputSanitizer.Sanitize(request.FullName),
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Phone = request.Phone,
            Role = fixedRole,
            IsActive = true,
            IsEmailVerified = false,
            LicenseNumber = request.LicenseNumber,
            Birthdate = string.IsNullOrEmpty(request.Birthdate) ? null : DateOnly.Parse(request.Birthdate).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var rawVerificationToken = Guid.NewGuid().ToString("N");
        user.EmailVerificationToken = HashToken(rawVerificationToken);
        await _userRepo.AddAsync(user);

        await _walletManager.CreateWalletAsync(user.Id);

        var verifyUrl = $"https://saiyad-eg.vercel.app/#/verify-email?token={rawVerificationToken}";
        await _emailService.SendAsync(
            user.Email,
            "Verify your Sayiad account",
            $@"<p>Hello {user.FullName},</p>
               <p>Please verify your email to activate your Sayiad account:</p>
               <p><a href='{verifyUrl}' style='background:#1a7f5a;color:#fff;padding:10px 20px;
               border-radius:6px;text-decoration:none;'>Verify Email</a></p>
               <p>If you did not register, ignore this email.</p>");

        _logger.LogInformation("User registered: {Email} as {Role}", user.Email, user.Role);
        await _auditService.LogAsync(user.Id, "Register", "User", user.Id, null, $"Role={user.Role}");
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
        await _auditService.LogAsync(user.Id, "Login", "User", user.Id);
        return await GenerateAuthResponse(user);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        var tokenHash = HashToken(refreshToken);
        var user = await _userRepo.GetByRefreshTokenAsync(tokenHash);

        if (user is null)
        {
            // Check if this was a replayed (stolen) token — matches previous hash
            user = await _userRepo.GetByPreviousRefreshTokenHashAsync(tokenHash);
            if (user is not null)
            {
                _logger.LogWarning("Refresh token replay detected for user {Email} — possible token theft. Invalidating all sessions.", user.Email);
                user.RefreshToken = null;
                user.PreviousRefreshTokenHash = null;
                user.RefreshTokenExpiry = null;
                await _userRepo.UpdateAsync(user);
                throw new UnauthorizedAccessException("Session compromised. Please log in again.");
            }

            throw new UnauthorizedAccessException("Invalid or expired refresh token");
        }

        // Before rotating, stash current hash for theft detection on next replay
        user.PreviousRefreshTokenHash = HashToken(refreshToken);

        _logger.LogInformation("Token refreshed for user: {Email}", user.Email);
        await _auditService.LogAsync(user.Id, "RefreshToken", "User", user.Id);
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
        await _auditService.LogAsync(userId, "Logout", "User", userId);
    }

    public async Task VerifyEmailAsync(string token)
    {
        var user = await _userRepo.GetByVerificationTokenAsync(HashToken(token))
            ?? throw new KeyNotFoundException("Invalid or expired verification token.");

        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user);

        _logger.LogInformation("Email verified for user: {UserId}", user.Id);
    }

    public async Task ResendVerificationAsync(string email)
    {
        var user = await _userRepo.GetByEmailAsync(email)
            ?? throw new KeyNotFoundException("User not found.");

        if (user.IsEmailVerified)
            throw new InvalidOperationException("Email is already verified.");

        var rawToken = Guid.NewGuid().ToString("N");
        user.EmailVerificationToken = HashToken(rawToken);
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user);

        var verifyUrl = $"https://saiyad-eg.vercel.app/#/verify-email?token={rawToken}";
        await _emailService.SendAsync(
            user.Email,
            "Verify your Sayiad account",
            $@"<p>Hello {user.FullName},</p>
               <p>Please verify your email to activate your Sayiad account:</p>
               <p><a href='{verifyUrl}' style='background:#1a7f5a;color:#fff;padding:10px 20px;
               border-radius:6px;text-decoration:none;'>Verify Email</a></p>
               <p>If you did not register, ignore this email.</p>");

        _logger.LogInformation("Verification email resent to: {Email}", user.Email);
    }

    public async Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _userRepo.GetByEmailAsync(request.Email);

        if (user is null)
        {
            _logger.LogInformation("Password reset requested for non-existent email: {Email}", request.Email);
            return Result.Failure("Email not found.");
        }

        var otp = Random.Shared.Next(100000, 999999).ToString();
        user.PasswordResetToken = BCrypt.Net.BCrypt.HashPassword(otp);
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user);

        await _emailService.SendAsync(
            user.Email,
            "Reset your Sayiad password",
            $@"<p>Hello {user.FullName},</p>
               <p>Your password reset code is:</p>
               <p style='font-size:24px;font-weight:bold;letter-spacing:4px;'>{otp}</p>
               <p>This code expires in 15 minutes.</p>
               <p>If you did not request a password reset, ignore this email.</p>");

        _logger.LogInformation("Password reset OTP sent to: {Email}", request.Email);
        return Result.Success();
    }

    public async Task<Result> VerifyResetCodeAsync(string email, string token)
    {
        var user = await _userRepo.GetByEmailAsync(email);

        if (user is null)
            return Result.Failure("Invalid reset attempt.");

        if (user.PasswordResetToken is null || user.PasswordResetTokenExpiry is null)
            return Result.Failure("No password reset was requested.");

        if (user.PasswordResetTokenExpiry < DateTime.UtcNow)
            return Result.Failure("Reset code has expired. Please request a new one.");

        if (!BCrypt.Net.BCrypt.Verify(token, user.PasswordResetToken))
            return Result.Failure("Invalid reset code.");

        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _userRepo.GetByEmailAsync(request.Email);

        if (user is null)
            return Result.Failure("Invalid reset attempt.");

        if (user.PasswordResetToken is null || user.PasswordResetTokenExpiry is null)
            return Result.Failure("No password reset was requested.");

        if (user.PasswordResetTokenExpiry < DateTime.UtcNow)
            return Result.Failure("Reset code has expired. Please request a new one.");

        if (!BCrypt.Net.BCrypt.Verify(request.Token, user.PasswordResetToken))
            return Result.Failure("Invalid reset code.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user);

        await _emailService.SendAsync(
            user.Email,
            "Your Sayiad password was reset",
            $@"<p>Hello {user.FullName},</p>
               <p>Your password has been reset successfully.</p>
               <p>If you did not make this change, contact support immediately.</p>");

        _logger.LogInformation("Password reset completed for: {Email}", request.Email);
        return Result.Success();
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

        user.RefreshToken = HashToken(refreshToken);
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

    private static string HashToken(string token)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}