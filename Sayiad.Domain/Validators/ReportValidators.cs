using FluentValidation;
using Sayiad.Domain.Dtos.ReportDtos;

namespace Sayiad.Domain.Validators;

public class SubmitReportValidator : AbstractValidator<SubmitReportRequest>
{
    public SubmitReportValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.TargetType).IsInEnum();
        RuleFor(x => x.Message).NotEmpty().MaximumLength(2000);
    }
}

public class ResolveReportValidator : AbstractValidator<ResolveReportRequest>
{
    public ResolveReportValidator()
    {
        RuleFor(x => x.NewStatus).IsInEnum();
        RuleFor(x => x.AdminNote).MaximumLength(1000);
    }
}
