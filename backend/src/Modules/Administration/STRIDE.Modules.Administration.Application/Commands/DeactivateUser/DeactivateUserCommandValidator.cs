using FluentValidation;

namespace STRIDE.Modules.Administration.Application.Commands.DeactivateUser;

internal sealed class DeactivateUserCommandValidator : AbstractValidator<DeactivateUserCommand>
{
    public DeactivateUserCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.CallerUserId).NotEmpty();
    }
}
