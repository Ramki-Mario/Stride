using FluentValidation;

namespace STRIDE.Modules.Reporting.Application.Queries.GetReportList;

public sealed class GetReportListQueryValidator : AbstractValidator<GetReportListQuery>
{
    public GetReportListQueryValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required.");
    }
}
