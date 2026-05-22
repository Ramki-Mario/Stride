using FluentValidation;

namespace STRIDE.Modules.Invoicing.Application.Commands.VoidInvoice;

public sealed class VoidInvoiceCommandValidator : AbstractValidator<VoidInvoiceCommand>
{
    public VoidInvoiceCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.InvoiceId).NotEmpty();
        RuleFor(c => c.VoidedBy).NotEmpty();
    }
}
