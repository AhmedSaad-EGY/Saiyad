using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sayiad.Domain.Dtos.AuthDtos;

namespace Sayiad.Api.Controllers;

/// <summary>
/// Handles user authentication: register, login, token refresh, logout,
/// email verification, password reset (forgot + reset), and password change.
/// </summary>
[ApiController]
    [Route("api/[controller]")]
public class AuthController : ControllerBase
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
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request)
    {
        var result = await _authManager.RefreshTokenAsync(request.RefreshToken);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _authManager.LogoutAsync(userId);
        return NoContent();
    }

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        await _authManager.VerifyEmailAsync(token);
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
        var result = await _authManager.ForgotPasswordAsync(request);

        if (!result.IsSuccess)
            return NotFound(new { message = result.Error });

        return Ok(new { message = "Reset code sent to your email." });
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
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _authManager.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
        return NoContent();
    }
}
