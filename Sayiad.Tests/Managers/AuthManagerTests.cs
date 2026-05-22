using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Sayiad.Tests.Managers;

public class AuthManagerTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<ILogger<AuthManager>> _loggerMock = new();
    private readonly Mock<IWalletManager> _walletManagerMock = new();
    private AuthManager CreateManager() =>
        new(_userRepoMock.Object, _tokenServiceMock.Object,
            _emailServiceMock.Object, _walletManagerMock.Object, _loggerMock.Object);

    [Fact]
    public async Task Register_WithAdminRole_ThrowsUnauthorizedAccessException()
    {
        var manager = CreateManager();
        var request = new RegisterRequest(
            "Test", "test@test.com", "Pass123!", "0123456789", "Admin");

        var act = () => manager.RegisterAsync(request);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Admin*");
    }

    [Fact]
    public async Task Register_WithExistingEmail_ThrowsInvalidOperationException()
    {
        _userRepoMock.Setup(r => r.EmailExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        var manager = CreateManager();
        var request = new RegisterRequest(
            "Test", "existing@test.com", "Pass123!", "0123456789", "Customer");

        var act = () => manager.RegisterAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already registered*");
    }

    [Fact]
    public async Task Login_WithUnverifiedEmail_ThrowsUnauthorizedAccessException()
    {
        var user = new User
        {
            Email = "test@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass123!"),
            IsActive = true,
            IsEmailVerified = false
        };
        _userRepoMock.Setup(r => r.GetByEmailAsync("test@test.com"))
            .ReturnsAsync(user);
        var manager = CreateManager();

        var act = () => manager.LoginAsync(
            new LoginRequest("test@test.com", "Pass123!"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*verify your email*");
    }

    [Fact]
    public async Task Login_WithInactiveAccount_ThrowsUnauthorizedAccessException()
    {
        var user = new User
        {
            Email = "test@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass123!"),
            IsActive = false,
            IsEmailVerified = true
        };
        _userRepoMock.Setup(r => r.GetByEmailAsync("test@test.com"))
            .ReturnsAsync(user);
        var manager = CreateManager();

        var act = () => manager.LoginAsync(
            new LoginRequest("test@test.com", "Pass123!"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*disabled*");
    }
}
