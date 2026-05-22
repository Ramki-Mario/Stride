using FluentValidation;

namespace STRIDE.Modules.Invoicing.Application.Commands.GenerateInvoice;

public sealed class GenerateInvoiceCommandValidator : AbstractValidator<GenerateInvoiceCommand>
{
    public GenerateInvoiceCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.InvoiceNumber).NotEmpty().MaximumLength(50);
        RuleFor(c => c.ClientName).NotEmpty().MaximumLength(200);
        RuleFor(c => c.ClientEmail).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(c => c.Currency).NotEmpty().Length(3);
        RuleFor(c => c.LineItems).NotEmpty().WithMessage("At least one line item is required.");
        RuleForEach(c => c.LineItems).ChildRules(item =>
        {
            item.RuleFor(l => l.Description).NotEmpty().MaximumLength(500);
            item.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
            item.RuleFor(l => l.Quantity).GreaterThan(0);
        });
    }
}
