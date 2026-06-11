namespace Sayiad.Api.Controllers;

/// <summary>
/// Handles user authentication: register, login, token refresh, logout,
/// email verification, password reset (forgot + reset), and password change.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : BaseController
{
    private readonly IAuthManager _authManager;

    public AuthController(IAuthManager authManager)
    {
        _authManager = authManager;
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await _authManager.RegisterAsync(request);
        return Created("", result);
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _authManager.LoginAsync(request);

        Response.Cookies.Append("sayiad_refreshToken", result.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/api/auth",
            Expires = DateTime.UtcNow.AddDays(7)
        });

        return Ok(new { token = result.Token, expiresAt = result.ExpiresAt, user = result.User });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["sayiad_refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { message = "Refresh token not found" });

        var result = await _authManager.RefreshTokenAsync(refreshToken);

        Response.Cookies.Append("sayiad_refreshToken", result.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/api/auth",
            Expires = DateTime.UtcNow.AddDays(7)
        });

        return Ok(new { token = result.Token, expiresAt = result.ExpiresAt, user = result.User });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = GetUserId();
        await _authManager.LogoutAsync(userId);
        return NoContent();
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        await _authManager.VerifyEmailAsync(request.Token);
        return Ok(new { message = "Email verified successfully. You can now log in." });
    }

    [HttpPost("resend-verification")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResendVerification(ResendVerificationRequest request)
    {
        await _authManager.ResendVerificationAsync(request.Email);
        return Ok(new { message = "Verification email sent." });
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        await _authManager.ForgotPasswordAsync(request);

        return Ok(new { message = "If that email is registered you will receive a reset code." });
    }

    [HttpPost("verify-reset-code")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> VerifyResetCode(VerifyResetCodeRequest request)
    {
        var result = await _authManager.VerifyResetCodeAsync(request.Email, request.Token);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(new { message = "Code verified." });
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var result = await _authManager.ResetPasswordAsync(request);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(new { message = "Password reset successful." });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var userId = GetUserId();
        await _authManager.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
        return NoContent();
    }
}
