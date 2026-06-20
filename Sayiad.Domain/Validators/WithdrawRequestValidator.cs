using FluentValidation;
using Sayiad.Domain.Dtos.WalletDtos;

namespace Sayiad.Domain.Validators;

public class WithdrawRequestValidator : AbstractValidator<WithdrawRequest>
{
    public WithdrawRequestValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Withdrawal amount must be positive.");
    }
}
