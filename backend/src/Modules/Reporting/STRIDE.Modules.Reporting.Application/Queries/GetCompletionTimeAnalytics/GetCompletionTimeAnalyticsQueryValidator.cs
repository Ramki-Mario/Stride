using FluentValidation;

namespace STRIDE.Modules.Reporting.Application.Queries.GetCompletionTimeAnalytics;

public sealed class GetCompletionTimeAnalyticsQueryValidator
    : AbstractValidator<GetCompletionTimeAnalyticsQuery>
{
    public GetCompletionTimeAnalyticsQueryValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required.");

        RuleFor(x => x.FromDate)
            .NotEmpty()
            .WithMessage("FromDate is required.");

        RuleFor(x => x.ToDate)
            .NotEmpty()
            .GreaterThan(x => x.FromDate)
            .WithMessage("ToDate must be after FromDate.");

        RuleFor(x => x)
            .Must(x => (x.ToDate - x.FromDate).TotalDays <= 366)
            .WithMessage("Date range may not exceed 366 days.");
    }
}
