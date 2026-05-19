using FluentValidation;

namespace STRIDE.Modules.Reporting.Application.Queries.GetWorkflowTrends;

public sealed class GetWorkflowTrendsQueryValidator : AbstractValidator<GetWorkflowTrendsQuery>
{
    public GetWorkflowTrendsQueryValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required.");

        RuleFor(x => x.Days)
            .InclusiveBetween(1, 365)
            .WithMessage("Days must be between 1 and 365.");
    }
}
