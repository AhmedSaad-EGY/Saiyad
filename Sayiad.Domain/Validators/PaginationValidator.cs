using FluentValidation;
using Sayiad.Data.Common;

namespace Sayiad.Domain.Validators;

public class PaginationValidator : AbstractValidator<PaginationRequest>
{
    public PaginationValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Page must be 1 or greater.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200).WithMessage("Page size must be between 1 and 200.");
    }
}
