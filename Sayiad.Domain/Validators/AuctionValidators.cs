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

public class ApproveAuctionRequestValidator : AbstractValidator<ApproveAuctionRequestRequest>
{
    public ApproveAuctionRequestValidator()
    {
        RuleFor(x => x.EndTime)
            .GreaterThan(DateTime.UtcNow).WithMessage("End time must be in the future.");
        RuleFor(x => x.StartingPrice)
            .GreaterThan(0).WithMessage("Starting price must be greater than 0.");
        RuleFor(x => x.ReservePrice)
            .GreaterThanOrEqualTo(0).WithMessage("Reserve price must not be negative.");
        RuleFor(x => x.MinimumIncrement)
            .GreaterThan(0).WithMessage("Minimum increment must be greater than 0.");
    }
}

public class RejectAuctionRequestValidator : AbstractValidator<RejectAuctionRequestRequest>
{
    public RejectAuctionRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000).WithMessage("Rejection reason is required.");
    }
}

public class AuctionFilterValidator : AbstractValidator<AuctionFilterRequest>
{
}
