using FluentAssertions;
using Sayiad.Domain.Dtos.WalletDtos;
using Sayiad.Domain.Validators;

namespace Sayiad.Tests.Validators;

public class WithdrawRequestValidatorTests
{
    private readonly WithdrawRequestValidator _validator = new();

    [Fact]
    public void Validate_WhenAmountIsPositive_Passes()
    {
        var result = _validator.Validate(new WithdrawRequest(10m));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenAmountIsZero_Fails()
    {
        var result = _validator.Validate(new WithdrawRequest(0m));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(WithdrawRequest.Amount) &&
            error.ErrorMessage == "Withdrawal amount must be positive.");
    }

    [Fact]
    public void Validate_WhenAmountIsNegative_Fails()
    {
        var result = _validator.Validate(new WithdrawRequest(-10m));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(WithdrawRequest.Amount) &&
            error.ErrorMessage == "Withdrawal amount must be positive.");
    }
}
