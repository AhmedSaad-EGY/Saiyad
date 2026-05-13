using FluentValidation;
using Sayiad.Domain.Dtos.SellerProfileDtos;

namespace Sayiad.Domain.Validators;

public class CreateSellerProfileValidator : AbstractValidator<CreateSellerProfileRequest>
{
    public CreateSellerProfileValidator()
    {
        RuleFor(x => x.StoreName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.ContactEmail).EmailAddress().MaximumLength(255);
        RuleFor(x => x.ContactPhone).MaximumLength(20);
        RuleFor(x => x.Location).MaximumLength(200);
    }
}

public class UpdateSellerProfileValidator : AbstractValidator<UpdateSellerProfileRequest>
{
    public UpdateSellerProfileValidator()
    {
        RuleFor(x => x.StoreName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.ContactEmail).EmailAddress().MaximumLength(255);
        RuleFor(x => x.ContactPhone).MaximumLength(20);
        RuleFor(x => x.Location).MaximumLength(200);
    }
}
