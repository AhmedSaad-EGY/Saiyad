using FluentValidation;
using Sayiad.Domain.Dtos.AuctionDtos;

namespace Sayiad.Domain.Validators;

public class SubmitAuctionRequestValidator : AbstractValidator<SubmitAuctionRequestRequest>
{
    public SubmitAuctionRequestValidator()
    {
        RuleFor(x => x.ProductTitle).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ProductDescription).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.EstimatedValue).GreaterThan(0);
        RuleFor(x => x.QuantityKg).GreaterThan(0);
        RuleFor(x => x.FishType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CatchLocation).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CatchDate).LessThanOrEqualTo(DateTime.UtcNow.AddDays(1)).When(x => x.CatchDate.HasValue);
    }
}
