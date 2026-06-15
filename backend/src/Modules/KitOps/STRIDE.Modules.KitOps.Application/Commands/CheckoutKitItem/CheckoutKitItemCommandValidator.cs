using FluentValidation;
using STRIDE.Modules.KitOps.Domain.Entities;

namespace STRIDE.Modules.KitOps.Application.Commands.CheckoutKitItem;

internal sealed class CheckoutKitItemCommandValidator : AbstractValidator<CheckoutKitItemCommand>
{
    public const int MaxDays = 365;

    public CheckoutKitItemCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.KitItemId).NotEmpty();
        RuleFor(x => x.CheckedOutByUserId).NotEmpty();
        RuleFor(x => x.Days)
            .InclusiveBetween(1, MaxDays);
        RuleFor(x => x.Notes)
            .MaximumLength(KitCheckout.NotesMaxLength)
            .When(x => x.Notes is not null);
    }
}
