using FluentValidation;
using STRIDE.Modules.KitOps.Domain.Entities;

namespace STRIDE.Modules.KitOps.Application.Commands.UpdateKitItem;

internal sealed class UpdateKitItemCommandValidator : AbstractValidator<UpdateKitItemCommand>
{
    public UpdateKitItemCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.KitItemId).NotEmpty();
        RuleFor(x => x.UpdatedBy).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(KitItem.NameMaxLength);
        RuleFor(x => x.Category)
            .NotEmpty()
            .MaximumLength(KitItem.CategoryMaxLength);
        RuleFor(x => x.Description)
            .MaximumLength(KitItem.DescriptionMaxLength)
            .When(x => x.Description is not null);
        RuleFor(x => x.TotalQuantity)
            .GreaterThan(0);
    }
}
