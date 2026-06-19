using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Sayiad.Api.Controllers;
using Sayiad.Domain.Dtos.WalletDtos;
using Sayiad.Domain.Validators;

namespace Sayiad.Tests.Controllers;

public class WalletControllerTests
{
    private const int UserId = 42;
    private readonly Mock<IWalletManager> _walletManager = new();

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
