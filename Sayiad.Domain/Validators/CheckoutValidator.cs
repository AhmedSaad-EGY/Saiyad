using FluentValidation;
using Sayiad.Domain.Dtos.OrderDtos;

namespace Sayiad.Domain.Validators;

public class CheckoutValidator : AbstractValidator<CheckoutRequest>
{
    public CheckoutValidator()
    {
        RuleFor(x => x.ShippingAddressId).GreaterThan(0);
        RuleFor(x => x.PaymentMethod).NotEmpty();
    }
}
