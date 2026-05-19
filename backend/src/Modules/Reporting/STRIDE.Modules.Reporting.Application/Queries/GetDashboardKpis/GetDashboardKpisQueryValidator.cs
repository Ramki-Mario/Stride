using FluentValidation;

namespace STRIDE.Modules.Reporting.Application.Queries.GetDashboardKpis;

public sealed class GetDashboardKpisQueryValidator : AbstractValidator<GetDashboardKpisQuery>
{
    public GetDashboardKpisQueryValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required.");
    }
}
