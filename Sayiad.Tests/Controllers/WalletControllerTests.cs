using System.Security.Claims;
using FluentValidation;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Sayiad.Api.Controllers;
using Sayiad.Api.Filters;
using Sayiad.Domain.Dtos.WalletDtos;
using Sayiad.Domain.Validators;

namespace Sayiad.Tests.Controllers;

public class WalletControllerTests
{
    private const int UserId = 42;
    private readonly Mock<IWalletManager> _walletManager = new();

    [Fact]
    public void DepositAuthorization_AllowsDemoRolesButExcludesAdmin()
    {
        var authorize = typeof(WalletController)
            .GetMethod(nameof(WalletController.Deposit))!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single();

        var roles = authorize.Roles!.Split(',', StringSplitOptions.TrimEntries);

        roles.Should().BeEquivalentTo(
            nameof(UserRole.Customer),
            nameof(UserRole.Fisherman),
            nameof(UserRole.BaitSeller),
            nameof(UserRole.Auctioneer));
        roles.Should().NotContain(nameof(UserRole.Admin));
    }

    [Fact]
    public void WithdrawAuthorization_AllowsWalletRolesIncludingAdmin()
    {
        var authorize = typeof(WalletController)
            .GetMethod(nameof(WalletController.Withdraw))!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single();

        var roles = authorize.Roles!.Split(',', StringSplitOptions.TrimEntries);

        roles.Should().BeEquivalentTo(
            nameof(UserRole.Customer),
            nameof(UserRole.Fisherman),
            nameof(UserRole.BaitSeller),
            nameof(UserRole.Auctioneer),
            nameof(UserRole.Admin));
    }

    [Fact]
    public async Task GetTransactions_WhenPaginationIsMissing_UsesSafeDefaults()
    {
        var expected = new WalletTransactionsResponse([], 0, 1, 20);
        _walletManager
            .Setup(manager => manager.GetTransactionsAsync(
                UserId,
                It.Is<PaginationRequest>(pagination =>
                    pagination.Page == 1 && pagination.PageSize == 20)))
            .ReturnsAsync(expected);

        var result = await CreateController().GetTransactions(null);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(expected);
        _walletManager.VerifyAll();
    }

    [Fact]
    public async Task GetTransactions_WhenPaginationIsProvided_PreservesQueryValues()
    {
        var pagination = new PaginationRequest { Page = 1, PageSize = 10 };
        var expected = new WalletTransactionsResponse([], 0, 1, 10);
        _walletManager
            .Setup(manager => manager.GetTransactionsAsync(UserId, pagination))
            .ReturnsAsync(expected);

        var result = await CreateController().GetTransactions(pagination);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(expected);
        _walletManager.VerifyAll();
    }

    [Fact]
    public void PaginationValidator_WhenValuesAreInvalid_ReturnsExistingErrors()
    {
        var result = new PaginationValidator().Validate(
            new PaginationRequest { Page = 0, PageSize = 201 });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(PaginationRequest.Page) &&
            error.ErrorMessage == "Page must be 1 or greater.");
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(PaginationRequest.PageSize) &&
            error.ErrorMessage == "Page size must be between 1 and 200.");
    }

    [Fact]
    public void RequireValidatorFilter_WhenWithdrawValidatorIsAssemblyScanned_AllowsRequest()
    {
        var services = new ServiceCollection();
        services.AddValidatorsFromAssemblyContaining<WithdrawRequestValidator>();
        using var provider = services.BuildServiceProvider();
        var filter = new RequireValidatorFilter(
            provider,
            Mock.Of<ILogger<RequireValidatorFilter>>());
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor(),
            new ModelStateDictionary());
        var filterContext = new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>
            {
                ["request"] = new WithdrawRequest(10m)
            },
            new object());

        filter.OnActionExecuting(filterContext);

        filterContext.Result.Should().BeNull();
        filterContext.ModelState.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Withdraw_WhenRequestIsValid_ForwardsAmountToManager()
    {
        var expected = new WalletResponse(90m, 0m, 90m, DateTime.UtcNow, null);
        _walletManager
            .Setup(manager => manager.WithdrawAsync(UserId, 10m))
            .ReturnsAsync(expected);

        var result = await CreateController().Withdraw(new WithdrawRequest(10m));

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(expected);
        _walletManager.VerifyAll();
    }

    private WalletController CreateController()
    {
        var controller = new WalletController(_walletManager.Object);
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, UserId.ToString())],
            "Test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
        return controller;
    }
}
