using FluentValidation;
using Sayiad.Domain.Dtos.AuthDtos;

namespace Sayiad.Domain.Validators.Auth;

public class VerifyEmailValidator : AbstractValidator<VerifyEmailRequest>
{
    public VerifyEmailValidator()
    {
        RuleFor(x => x.Token).NotEmpty().WithMessage("Verification token is required.");
    }
}
