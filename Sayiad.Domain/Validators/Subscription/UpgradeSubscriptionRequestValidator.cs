using FluentValidation;
using Sayiad.Domain.Dtos.Subscription;

namespace Sayiad.Domain.Validators.Subscription;

public class UpgradeSubscriptionRequestValidator : AbstractValidator<UpgradeSubscriptionRequest>
{
    public UpgradeSubscriptionRequestValidator()
    {
        RuleFor(x => x.Tier)
            .NotEmpty().WithMessage("Tier is required.")
            .Must(t => t is "Basic" or "Pro" or "Enterprise")
            .WithMessage("Tier must be one of: Basic, Pro, Enterprise.");

        RuleFor(x => x.PaymentReference)
            .NotEmpty().WithMessage("Payment reference is required.");
    }
}
