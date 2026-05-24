using FluentValidation;
using Sayiad.Domain.Dtos.ReportDtos;

namespace Sayiad.Domain.Validators;

public class CreateReportValidator : AbstractValidator<CreateReportRequest>
{
    public CreateReportValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
