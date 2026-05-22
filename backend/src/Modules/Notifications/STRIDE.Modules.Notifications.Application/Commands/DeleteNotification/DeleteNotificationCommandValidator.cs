using FluentValidation;

namespace STRIDE.Modules.Notifications.Application.Commands.DeleteNotification;

public sealed class DeleteNotificationCommandValidator : AbstractValidator<DeleteNotificationCommand>
{
    public DeleteNotificationCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(x => x.NotificationId)
            .NotEmpty().WithMessage("NotificationId is required.");

        RuleFor(x => x.RecipientId)
            .NotEmpty().WithMessage("RecipientId is required.");
    }
}
