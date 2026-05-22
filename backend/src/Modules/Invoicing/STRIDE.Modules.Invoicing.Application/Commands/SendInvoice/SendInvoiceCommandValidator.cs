using FluentValidation;

namespace STRIDE.Modules.Invoicing.Application.Commands.SendInvoice;

public sealed class SendInvoiceCommandValidator : AbstractValidator<SendInvoiceCommand>
{
    public SendInvoiceCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.InvoiceId).NotEmpty();
        RuleFor(c => c.SentBy).NotEmpty();
    }
}
