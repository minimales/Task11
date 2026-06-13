using FluentValidation;
using task11.ApplicationCore.Models;

namespace task11.ApplicationCore.Validators;

public class PeriodReportModelValidator : AbstractValidator<PeriodReportModel>
{
    private const int _maxSpanDays = 366;

    public PeriodReportModelValidator()
    {
        RuleFor(x => x.WalletId)
            .NotEmpty()
            .WithMessage("WalletId is required.");

        RuleFor(x => x.StartDate)
            .LessThanOrEqualTo(x => x.EndDate)
            .WithMessage("StartDate must be on or before EndDate.");

        RuleFor(x => x)
            .Must(HaveSpanWithinLimit)
            .When(x => x.StartDate <= x.EndDate)
            .WithName(nameof(PeriodReportModel.EndDate))
            .WithMessage($"The reporting period must not exceed {_maxSpanDays} days.");
    }

    private static bool HaveSpanWithinLimit(PeriodReportModel request)
    {

        var spanDays = (request.EndDate.Date - request.StartDate.Date).TotalDays + 1;
        return spanDays <= _maxSpanDays;
    }
}
