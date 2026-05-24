using FluentValidation;
using Sayiad.Domain.Dtos.UserDtos;

namespace Sayiad.Domain.Validators;

public class UpdateUserValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MinimumLength(2).MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty();
    }
}
