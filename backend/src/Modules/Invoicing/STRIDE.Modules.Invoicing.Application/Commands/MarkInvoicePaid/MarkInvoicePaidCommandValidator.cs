using FluentValidation;

namespace STRIDE.Modules.Invoicing.Application.Commands.MarkInvoicePaid;

public sealed class MarkInvoicePaidCommandValidator : AbstractValidator<MarkInvoicePaidCommand>
{
    public MarkInvoicePaidCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.InvoiceId).NotEmpty();
        RuleFor(c => c.MarkedBy).NotEmpty();
    }
}
