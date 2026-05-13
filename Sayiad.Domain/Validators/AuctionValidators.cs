using FluentValidation;
using Sayiad.Domain.Dtos.AuctionDtos;

namespace Sayiad.Domain.Validators;

public class CreateAuctionValidator : AbstractValidator<CreateAuctionRequest>
{
    public CreateAuctionValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.EndTime).GreaterThan(DateTime.UtcNow.AddHours(1));
        RuleFor(x => x.StartingPrice).GreaterThan(0);
        RuleFor(x => x.ReservePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinimumIncrement).GreaterThan(0);
    }
}

public class PlaceBidValidator : AbstractValidator<PlaceBidRequest>
{
    public PlaceBidValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}
