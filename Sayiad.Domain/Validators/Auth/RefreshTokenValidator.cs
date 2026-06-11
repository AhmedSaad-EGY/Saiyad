using FluentValidation;
using Sayiad.Domain.Dtos.AuthDtos;

namespace Sayiad.Domain.Validators.Auth;

public class RefreshTokenValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
