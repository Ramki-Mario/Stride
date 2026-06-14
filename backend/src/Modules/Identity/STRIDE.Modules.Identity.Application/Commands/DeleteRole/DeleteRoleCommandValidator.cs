using FluentValidation;

namespace STRIDE.Modules.Identity.Application.Commands.DeleteRole;

public sealed class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("RoleId is required.");

        RuleFor(x => x.ActorId)
            .NotEmpty().WithMessage("ActorId is required.");
    }
}
