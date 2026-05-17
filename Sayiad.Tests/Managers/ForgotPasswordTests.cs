using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Sayiad.Tests.Managers;

public class ForgotPasswordTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<ILogger<AuthManager>> _loggerMock = new();

    private AuthManager CreateManager() =>
        new(_userRepoMock.Object, _tokenServiceMock.Object,
            _emailServiceMock.Object, _loggerMock.Object);

    [Fact]
    public async Task ForgotPassword_WithExistingEmail_SendsEmailAndReturnsSuccess()
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync("test@test.com"))
            .ReturnsAsync(new User { Id = 1, Email = "test@test.com", FullName = "Test" });
        var manager = CreateManager();

        var result = await manager.ForgotPasswordAsync(
            new ForgotPasswordRequest("test@test.com"));

        result.IsSuccess.Should().BeTrue();
        _emailServiceMock.Verify(e => e.SendAsync("test@test.com",
            It.Is<string>(s => s.Contains("Reset")),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_WithNonExistentEmail_ReturnsSuccessWithoutSending()
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);
        var manager = CreateManager();

        var result = await manager.ForgotPasswordAsync(
            new ForgotPasswordRequest("missing@test.com"));

        result.IsSuccess.Should().BeTrue();
        _emailServiceMock.Verify(e => e.SendAsync(It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_ResetsPassword()
    {
        var otpHash = BCrypt.Net.BCrypt.HashPassword("123456");
        var user = new User
        {
            Id = 1,
            Email = "test@test.com",
            FullName = "Test",
            PasswordResetToken = otpHash,
            PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(15),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass123!")
        };
        _userRepoMock.Setup(r => r.GetByEmailAsync("test@test.com"))
            .ReturnsAsync(user);
        var manager = CreateManager();

        var result = await manager.ResetPasswordAsync(
            new ResetPasswordRequest("test@test.com", "123456", "NewPass123!", "NewPass123!"));

        result.IsSuccess.Should().BeTrue();
        user.PasswordResetToken.Should().BeNull();
        user.PasswordResetTokenExpiry.Should().BeNull();
    }

    [Fact]
    public async Task ResetPassword_WithExpiredToken_ReturnsFailure()
    {
        var otpHash = BCrypt.Net.BCrypt.HashPassword("123456");
        var user = new User
        {
            Id = 1,
            Email = "test@test.com",
            PasswordResetToken = otpHash,
            PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(-5)
        };
        _userRepoMock.Setup(r => r.GetByEmailAsync("test@test.com"))
            .ReturnsAsync(user);
        var manager = CreateManager();

        var result = await manager.ResetPasswordAsync(
            new ResetPasswordRequest("test@test.com", "123456", "NewPass123!", "NewPass123!"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("expired");
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_ReturnsFailure()
    {
        var otpHash = BCrypt.Net.BCrypt.HashPassword("654321");
        var user = new User
        {
            Id = 1,
            Email = "test@test.com",
            PasswordResetToken = otpHash,
            PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(15)
        };
        _userRepoMock.Setup(r => r.GetByEmailAsync("test@test.com"))
            .ReturnsAsync(user);
        var manager = CreateManager();

        var result = await manager.ResetPasswordAsync(
            new ResetPasswordRequest("test@test.com", "000000", "NewPass123!", "NewPass123!"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid reset code");
    }
}
