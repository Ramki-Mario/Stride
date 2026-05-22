using FluentValidation;

namespace STRIDE.Modules.Administration.Application.Commands.UpdateUserRole;

internal sealed class UpdateUserRoleCommandValidator : AbstractValidator<UpdateUserRoleCommand>
{
    private static readonly string[] ValidRoles = ["Admin", "Member", "Viewer"];

    public UpdateUserRoleCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NewRole).NotEmpty().Must(r => ValidRoles.Contains(r))
            .WithMessage("Role must be Admin, Member, or Viewer.");
        RuleFor(x => x.UpdatedBy).NotEmpty();
    }
}
