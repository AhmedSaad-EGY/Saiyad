using FluentValidation;
using Sayiad.Domain.Dtos.SubscriptionPlanDtos;

namespace Sayiad.Domain.Validators;

public class CreateSubscriptionPlanValidator : AbstractValidator<CreateSubscriptionPlanRequest>
{
    public CreateSubscriptionPlanValidator()
    {
        RuleFor(x => x.Tier).NotEmpty().WithMessage("Tier is required.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be greater than 0.");
        RuleFor(x => x.Currency).NotEmpty().Length(3).WithMessage("Currency must be a 3-letter code.");
        RuleFor(x => x.BillingCycle).NotEmpty().WithMessage("Billing cycle is required.");
        RuleFor(x => x.MaxAuctionsPerMonth).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxBidsPerMonth).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxAuctionRequestsPerMonth).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Features).NotNull().WithMessage("Features list is required.");
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public class UpdateSubscriptionPlanValidator : AbstractValidator<UpdateSubscriptionPlanRequest>
{
    public UpdateSubscriptionPlanValidator()
    {
        When(x => x.Name is not null, () => RuleFor(x => x.Name).NotEmpty().MaximumLength(100));
        When(x => x.Price is not null, () => RuleFor(x => x.Price).GreaterThan(0));
        When(x => x.Currency is not null, () => RuleFor(x => x.Currency).NotEmpty().Length(3));
        When(x => x.BillingCycle is not null, () => RuleFor(x => x.BillingCycle).NotEmpty());
        When(x => x.MaxAuctionsPerMonth is not null, () => RuleFor(x => x.MaxAuctionsPerMonth).GreaterThanOrEqualTo(0));
        When(x => x.MaxBidsPerMonth is not null, () => RuleFor(x => x.MaxBidsPerMonth).GreaterThanOrEqualTo(0));
        When(x => x.MaxAuctionRequestsPerMonth is not null, () => RuleFor(x => x.MaxAuctionRequestsPerMonth).GreaterThanOrEqualTo(0));
        When(x => x.SortOrder is not null, () => RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0));
    }
}
