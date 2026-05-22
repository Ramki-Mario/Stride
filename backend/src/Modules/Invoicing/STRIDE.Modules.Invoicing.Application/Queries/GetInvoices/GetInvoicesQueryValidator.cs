using FluentValidation;

namespace STRIDE.Modules.Invoicing.Application.Queries.GetInvoices;

public sealed class GetInvoicesQueryValidator : AbstractValidator<GetInvoicesQuery>
{
    public GetInvoicesQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, 100);
    }
}
