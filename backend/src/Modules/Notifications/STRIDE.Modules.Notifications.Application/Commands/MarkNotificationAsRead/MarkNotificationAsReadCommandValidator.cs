using FluentValidation;

namespace STRIDE.Modules.Notifications.Application.Commands.MarkNotificationAsRead;

public sealed class MarkNotificationAsReadCommandValidator : AbstractValidator<MarkNotificationAsReadCommand>
{
    public MarkNotificationAsReadCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(x => x.NotificationId)
            .NotEmpty().WithMessage("NotificationId is required.");

        RuleFor(x => x.RecipientId)
            .NotEmpty().WithMessage("RecipientId is required.");
    }
}
