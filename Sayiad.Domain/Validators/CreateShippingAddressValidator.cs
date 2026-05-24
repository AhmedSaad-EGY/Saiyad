using FluentValidation;
using Sayiad.Domain.Dtos.ShippingAddressDtos;

namespace Sayiad.Domain.Validators;

public class CreateShippingAddressRequestValidator : AbstractValidator<CreateShippingAddressRequest>
{
    public CreateShippingAddressRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.City).NotEmpty().MaximumLength(50);
        RuleFor(x => x.AddressLine).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PostalCode).MaximumLength(20);
    }
}
