using FluentValidation;
using STRIDE.Modules.KitOps.Domain.Entities;

namespace STRIDE.Modules.KitOps.Application.Commands.CreateKitItem;

internal sealed class CreateKitItemCommandValidator : AbstractValidator<CreateKitItemCommand>
{
    public CreateKitItemCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CreatedBy).NotEmpty();
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
