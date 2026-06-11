using FluentValidation;
using Sayiad.Domain.Dtos.PaymentDtos;

namespace Sayiad.Domain.Validators;

public class InitiatePaymentValidator : AbstractValidator<InitiatePaymentRequest>
{
    public InitiatePaymentValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0).WithMessage("Order ID must be greater than 0.");
        RuleFor(x => x.PaymentMethod).NotEmpty().WithMessage("Payment method is required.");
    }
}
