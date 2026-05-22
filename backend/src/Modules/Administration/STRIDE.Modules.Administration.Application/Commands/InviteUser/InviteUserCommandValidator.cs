using FluentValidation;

namespace STRIDE.Modules.Administration.Application.Commands.InviteUser;

internal sealed class InviteUserCommandValidator : AbstractValidator<InviteUserCommand>
{
    private static readonly string[] ValidRoles =
        ["Admin", "OperationsManager", "FinanceUser", "FieldWorker", "Supervisor"];

    public InviteUserCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Role).NotEmpty().Must(r => ValidRoles.Contains(r))
            .WithMessage("Role must be one of: Admin, OperationsManager, FinanceUser, FieldWorker, Supervisor.");
        RuleFor(x => x.InvitedBy).NotEmpty();
    }
}
