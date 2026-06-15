using FluentValidation;
using STRIDE.Modules.KitOps.Domain.Entities;

namespace STRIDE.Modules.KitOps.Application.Commands.CreateKitReservation;

internal sealed class CreateKitReservationCommandValidator : AbstractValidator<CreateKitReservationCommand>
{
    public CreateKitReservationCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.KitItemId).NotEmpty();
        RuleFor(x => x.RequestedByUserId).NotEmpty();
        RuleFor(x => x.Notes)
            .MaximumLength(KitReservation.NotesMaxLength)
            .When(x => x.Notes is not null);
    }
}
