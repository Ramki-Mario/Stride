using FluentValidation;

namespace STRIDE.Modules.Reporting.Application.Queries.ExportReportCsv;

public sealed class ExportReportCsvQueryValidator : AbstractValidator<ExportReportCsvQuery>
{
    public ExportReportCsvQueryValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required.");

        RuleFor(x => x.ReportId)
            .NotEmpty()
            .WithMessage("ReportId is required.");
    }
}
