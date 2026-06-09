using FluentValidation;

namespace STRIDE.Modules.Notifications.Application.Commands.Push;

internal sealed class RegisterPushSubscriptionCommandValidator
    : AbstractValidator<RegisterPushSubscriptionCommand>
{
    public RegisterPushSubscriptionCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Endpoint).NotEmpty().MaximumLength(2048);
        RuleFor(x => x.P256dh).NotEmpty().MaximumLength(512);
        RuleFor(x => x.Auth).NotEmpty().MaximumLength(128);
    }
}
