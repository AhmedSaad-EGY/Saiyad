using FluentValidation;
using Sayiad.Domain.Dtos.AuthDtos;

namespace Sayiad.Domain.Validators.Auth;

public class VerifyResetCodeValidator : AbstractValidator<VerifyResetCodeRequest>
{
    public VerifyResetCodeValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");
        RuleFor(x => x.Token).NotEmpty().WithMessage("Verification token is required.");
    }
}
