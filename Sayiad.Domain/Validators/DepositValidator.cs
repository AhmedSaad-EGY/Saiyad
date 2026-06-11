using FluentValidation;
using Sayiad.Domain.Dtos.WalletDtos;

namespace Sayiad.Domain.Validators;

public class DepositValidator : AbstractValidator<DepositRequest>
{
    public DepositValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Deposit amount must be positive.");
    }
}
