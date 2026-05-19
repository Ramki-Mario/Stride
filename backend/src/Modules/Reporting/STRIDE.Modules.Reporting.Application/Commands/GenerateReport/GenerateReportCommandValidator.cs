using FluentValidation;
using STRIDE.Modules.Reporting.Domain.Entities;

namespace STRIDE.Modules.Reporting.Application.Commands.GenerateReport;

public sealed class GenerateReportCommandValidator : AbstractValidator<GenerateReportCommand>
{
    public GenerateReportCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required.");

        RuleFor(x => x.RequestedBy)
            .NotEmpty()
            .WithMessage("RequestedBy (UserId) is required.");

        RuleFor(x => x.ReportType)
            .IsInEnum()
            .WithMessage("ReportType must be a valid value (DashboardKpi, WorkflowTrend, WorkflowSummary).");

        RuleFor(x => x.TrendDays)
            .InclusiveBetween(1, 365)
            .WithMessage("TrendDays must be between 1 and 365.")
            .When(x => x.ReportType == ReportType.WorkflowTrend);
    }
}
